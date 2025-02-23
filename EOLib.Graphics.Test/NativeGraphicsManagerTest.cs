﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework;
using Microsoft.Xna.Framework.Graphics;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;

namespace EOLib.Graphics.Test
{
    [TestFixture, ExcludeFromCodeCoverage, Category("GraphicsDevice")]
    public class NativeGraphicsManagerTest
    {
        private GraphicsDeviceTestHelper _graphicsDeviceTestHelper;

        private INativeGraphicsLoader _graphicsLoader;
        private IGraphicsDeviceProvider _graphicsDeviceProvider;

        private bool _keepFromInfiniteLoop = false;

        private INativeGraphicsManager _nativeGraphicsManager;

        [SetUp]
        public void SetUp()
        {
            _graphicsDeviceTestHelper = new GraphicsDeviceTestHelper();

            _graphicsLoader = Mock.Of<INativeGraphicsLoader>();

            var graphicsDeviceProviderMock = new Mock<IGraphicsDeviceProvider>();
            graphicsDeviceProviderMock.Setup(x => x.GraphicsDevice)
                                      .Returns(_graphicsDeviceTestHelper.GraphicsDeviceManager.GraphicsDevice);

            _graphicsDeviceProvider = graphicsDeviceProviderMock.Object;

            _nativeGraphicsManager = new NativeGraphicsManager(_graphicsLoader, _graphicsDeviceProvider);
        }

        [TearDown]
        public void TearDown()
        {
            _nativeGraphicsManager.Dispose();
            _graphicsDeviceTestHelper.Dispose();
        }

        [Test]
        public void WhenLoadTexture_CallGraphicsLoader()
        {
            const int requestedResource = 1;

            var graphicsLoaderMock = Mock.Get(_graphicsLoader);
            using (LoadGFXReturnsBitmap(GFXTypes.PreLoginUI, requestedResource))
                _nativeGraphicsManager.TextureFromResource(GFXTypes.PreLoginUI, requestedResource);

            graphicsLoaderMock.Verify(x => x.LoadGFX(GFXTypes.PreLoginUI, requestedResource), Times.Once());
        }

        [Test]
        public void WhenLoadCachedTexture_DoNotCallGraphicsLoader()
        {
            const int requestedResource = 1;

            var graphicsLoaderMock = Mock.Get(_graphicsLoader);
            using (LoadGFXReturnsBitmap(GFXTypes.PreLoginUI, requestedResource))
            {
                _nativeGraphicsManager.TextureFromResource(GFXTypes.PreLoginUI, requestedResource);
                _nativeGraphicsManager.TextureFromResource(GFXTypes.PreLoginUI, requestedResource);
            }

            graphicsLoaderMock.Verify(x => x.LoadGFX(GFXTypes.PreLoginUI, requestedResource), Times.Once());
        }

        [Test]
        public void WhenLoadCachedTexture_WhenReloadFromFile_CallGraphicsLoader()
        {
            const int requestedResource = 1;

            var graphicsLoaderMock = Mock.Get(_graphicsLoader);
            using (LoadGFXReturnsBitmap(GFXTypes.PreLoginUI, requestedResource))
                _nativeGraphicsManager.TextureFromResource(GFXTypes.PreLoginUI, requestedResource);
            using (LoadGFXReturnsBitmap(GFXTypes.PreLoginUI, requestedResource))
                _nativeGraphicsManager.TextureFromResource(GFXTypes.PreLoginUI, requestedResource, reloadFromFile: true);

            graphicsLoaderMock.Verify(x => x.LoadGFX(GFXTypes.PreLoginUI, requestedResource), Times.Exactly(2));
        }

        [Test]
        public void WhenLoadCachedTexture_WhenReloadFromFile_DisposesOriginalTextue()
        {
            const int requestedResource = 1;

            var textureHasBeenDisposed = false;

            using (LoadGFXReturnsBitmap(GFXTypes.PreLoginUI, requestedResource))
            {
                var texture = _nativeGraphicsManager.TextureFromResource(GFXTypes.PreLoginUI, requestedResource);
                texture.Disposing += (o, e) => textureHasBeenDisposed = true;
            }

            using (LoadGFXReturnsBitmap(GFXTypes.PreLoginUI, requestedResource))
                _nativeGraphicsManager.TextureFromResource(GFXTypes.PreLoginUI, requestedResource, reloadFromFile: true);

            Assert.IsTrue(textureHasBeenDisposed);
        }

        [Test]
        public void WhenLoadManyTextures_CallsGraphicsLoaderSameNumberOfTimes()
        {
            var graphicsLoaderMock = Mock.Get(_graphicsLoader);

            const int totalRequestedResources = 100;
            for (int requestedResource = 1; requestedResource <= totalRequestedResources; ++requestedResource)
            {
                using (LoadGFXReturnsBitmap(GFXTypes.PreLoginUI, requestedResource))
                    _nativeGraphicsManager.TextureFromResource(GFXTypes.PreLoginUI, requestedResource);

                var localRequestedResource = requestedResource;
                graphicsLoaderMock.Verify(x => x.LoadGFX(GFXTypes.PreLoginUI, localRequestedResource), Times.Once());
            }

            graphicsLoaderMock.Verify(x => x.LoadGFX(GFXTypes.PreLoginUI, It.IsAny<int>()), Times.Exactly(totalRequestedResources));
        }

        [Test]
        public void WhenLoadCachedTexture_ManyTimes_CallsGraphicsLoaderOnce()
        {
            var graphicsLoaderMock = Mock.Get(_graphicsLoader);

            const int requestedResource = 1;
            for (int i = 1; i <= 100; ++i)
                using (LoadGFXReturnsBitmap(GFXTypes.PreLoginUI, requestedResource))
                    _nativeGraphicsManager.TextureFromResource(GFXTypes.PreLoginUI, requestedResource);

            graphicsLoaderMock.Verify(x => x.LoadGFX(GFXTypes.PreLoginUI, It.IsAny<int>()), Times.Once());
        }

        [Test]
        public void WhenLoadTexture_Transparent_SetsBlackToTransparent()
        {
            const int requestedResource = 1;

            Texture2D resultTexture;
            using (var bmp = LoadGFXReturnsBitmap(GFXTypes.PreLoginUI, requestedResource))
            {
                FillBitmapWithColor(bmp, Color.Black);
                resultTexture = _nativeGraphicsManager.TextureFromResource(GFXTypes.PreLoginUI, requestedResource, true);
            }

            var data = new Microsoft.Xna.Framework.Color[resultTexture.Width*resultTexture.Height];
            resultTexture.GetData(data);

            Assert.IsTrue(data.All(x => x.A == 0));
        }

        [Test]
        public void WhenLoadTexture_MaleHat_Transparent_SetsSpecialColorToTransparent()
        {
            const int requestedResource = 1;

            Texture2D resultTexture;
            using (var bmp = LoadGFXReturnsBitmap(GFXTypes.MaleHat, requestedResource))
            {
                FillBitmapWithColor(bmp, Color.FromRgba(0x08, 0x00, 0x00, 0xff));
                resultTexture = _nativeGraphicsManager.TextureFromResource(GFXTypes.MaleHat, requestedResource, true);
            }

            var data = new Microsoft.Xna.Framework.Color[resultTexture.Width * resultTexture.Height];
            resultTexture.GetData(data);

            Assert.IsTrue(data.All(x => x.A == 0));
        }

        [Test]
        public void WhenLoadTexture_FemaleHat_Transparent_SetsSpecialColorToTransparent()
        {
            const int requestedResource = 1;

            Texture2D resultTexture;
            using (var bmp = LoadGFXReturnsBitmap(GFXTypes.FemaleHat, requestedResource))
            {
                FillBitmapWithColor(bmp, Color.FromRgba(0x08, 0x00, 0x00, 0xff));
                resultTexture = _nativeGraphicsManager.TextureFromResource(GFXTypes.FemaleHat, requestedResource, true);
            }

            var data = new Microsoft.Xna.Framework.Color[resultTexture.Width * resultTexture.Height];
            resultTexture.GetData(data);

            Assert.IsTrue(data.All(x => x.A == 0));
        }

        [Test, Ignore("TODO: This is broken, probably because my computer is fast now")]
        public void WhenLoadTexture_RaceCondition_DisposesExistingCachedTextureAndReturnsSecondOne()
        {
            const int requestedResource = 1;

            Texture2D resultTexture;
            using (LoadGFXReturnsBitmap(GFXTypes.PreLoginUI, requestedResource, () => GetTextureAgain(GFXTypes.PreLoginUI, requestedResource)))
                resultTexture = _nativeGraphicsManager.TextureFromResource(GFXTypes.PreLoginUI, requestedResource);

            Assert.IsFalse(resultTexture.IsDisposed);
        }

        private IImage LoadGFXReturnsBitmap(GFXTypes whichFile, int requestedResource, Action loadCallback = null)
        {
            var bitmapToReturn = new Image<Rgba32>(10, 10);

            var graphicsLoaderMock = Mock.Get(_graphicsLoader);
            graphicsLoaderMock.Setup(x => x.LoadGFX(whichFile, requestedResource))
                .Returns(bitmapToReturn)
                .Callback(loadCallback ?? (() => { }));

            return bitmapToReturn;
        }

        private static void FillBitmapWithColor(IImage image, Color color)
        {
            var colorBrush = new SolidBrush(color);
            ((Image)image).Mutate(x => x.Clear(colorBrush));
        }

        private void GetTextureAgain(GFXTypes whichFile, int requestedResource)
        {
            if (_keepFromInfiniteLoop) return;
            _keepFromInfiniteLoop = true;

            using (LoadGFXReturnsBitmap(whichFile, requestedResource))
                _nativeGraphicsManager.TextureFromResource(whichFile, requestedResource);
        }
    }
}
