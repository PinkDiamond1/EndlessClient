using AutomaticTypeMapper;
using EndlessClient.Rendering.NPC;
using EOLib;
using EOLib.Domain.NPC;
using EOLib.Graphics;
using Microsoft.Xna.Framework.Graphics;

namespace EndlessClient.Rendering.Sprites
{
    [AutoMappedType]
    public class NPCSpriteSheet : INPCSpriteSheet
    {
        private readonly INativeGraphicsManager _gfxManager;
        private readonly INPCMetadataProvider _npcSpriteOffsetProvider;
        private readonly INPCMetadataLoader _npcMetadataLoader;

        public NPCSpriteSheet(INativeGraphicsManager gfxManager,
                              INPCMetadataProvider npcSpriteOffsetProvider,
                              INPCMetadataLoader npcMetadataLoader)
        {
            _gfxManager = gfxManager;
            _npcSpriteOffsetProvider = npcSpriteOffsetProvider;
            _npcMetadataLoader = npcMetadataLoader;
        }

        public Texture2D GetNPCTexture(int baseGraphic, NPCFrame whichFrame, EODirection direction)
        {
            int offset;
            switch (whichFrame)
            {
                case NPCFrame.Standing:
                    offset = direction == EODirection.Down || direction == EODirection.Right ? 1 : 3;
                    break;
                case NPCFrame.StandingFrame1:
                    offset = direction == EODirection.Down || direction == EODirection.Right ? 2 : 4;
                    break;
                case NPCFrame.WalkFrame1:
                    offset = direction == EODirection.Down || direction == EODirection.Right ? 5 : 9;
                    break;
                case NPCFrame.WalkFrame2:
                    offset = direction == EODirection.Down || direction == EODirection.Right ? 6 : 10;
                    break;
                case NPCFrame.WalkFrame3:
                    offset = direction == EODirection.Down || direction == EODirection.Right ? 7 : 11;
                    break;
                case NPCFrame.WalkFrame4:
                    offset = direction == EODirection.Down || direction == EODirection.Right ? 8 : 12;
                    break;
                case NPCFrame.Attack1:
                    offset = direction == EODirection.Down || direction == EODirection.Right ? 13 : 15;
                    break;
                case NPCFrame.Attack2:
                    offset = direction == EODirection.Down || direction == EODirection.Right ? 14 : 16;
                    break;
                default:
                    return null;
            }

            var baseGfx = (baseGraphic - 1) * 40;
            return _gfxManager.TextureFromResource(GFXTypes.NPC, baseGfx + offset, true);
        }

        public NPCMetadata GetNPCMetadata(int graphic)
        {
            var emptyMetadata = new NPCMetadata.Builder().ToImmutable();

            return _npcMetadataLoader.GetMetadata(graphic)
                .ValueOr(_npcSpriteOffsetProvider.DefaultMetadata.TryGetValue(graphic, out var ret) ? ret : emptyMetadata);
        }
    }

    public interface INPCSpriteSheet
    {
        Texture2D GetNPCTexture(int baseGraphic, NPCFrame whichFrame, EODirection direction);

        NPCMetadata GetNPCMetadata(int graphic);
    }
}