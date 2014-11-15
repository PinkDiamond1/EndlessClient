﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using EOLib.Data;
using Microsoft.Win32;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using XNAControls;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Color = Microsoft.Xna.Framework.Color;

namespace EndlessClient
{
	//This is going to be a single item in the inventory. will handle it's own drag/drop, onmouseover, etc.
	public class EOInventoryItem : XNAControl
	{
		private readonly ItemRecord m_itemData;
		public ItemRecord ItemData
		{
			get { return m_itemData; }
		}

		private InventoryItem m_inventory;
		public InventoryItem Inventory { get { return m_inventory; } set { m_inventory = value; } }

		public int Slot { get; private set; }

		private readonly Texture2D m_itemgfx, m_highlightBG;
		private XNALabel m_nameLabel;

		private bool m_beingDragged;
		public bool Dragging
		{
			get { return m_beingDragged; }
		}

		private int m_recentClickCount;
		private readonly Timer m_recentClickTimer;

		public EOInventoryItem(Game g, int slot, ItemRecord itemData, InventoryItem itemInventoryInfo, EOInventory inventory)
			: base(g, null, null, inventory)
		{
			m_itemData = itemData;
			m_inventory = itemInventoryInfo;
			Slot = slot;

			UpdateItemLocation(Slot);

			m_itemgfx = GFXLoader.TextureFromResource(GFXTypes.Items, 2 * itemData.Graphic, true);

			m_highlightBG = new Texture2D(g.GraphicsDevice, DrawArea.Width - 3, DrawArea.Height - 3);
			Color[] highlight = new Color[(drawArea.Width - 3) * (drawArea.Height - 3)];
			for (int i = 0; i < highlight.Length; ++i) { highlight[i] = Color.FromNonPremultiplied(200, 200, 200, 60); }
			m_highlightBG.SetData(highlight);
			
			_initItemLabel();

			m_recentClickTimer = new Timer(
				_state => { if (m_recentClickCount > 0) Interlocked.Decrement(ref m_recentClickCount); }, null, 0, 750);
		}

		public override void Update(GameTime gameTime)
		{
			//check for drag-drop here
			MouseState currentState = Mouse.GetState();

			if (MouseOverPreviously && MouseOver && PreviousMouseState.LeftButton == ButtonState.Pressed && currentState.LeftButton == ButtonState.Pressed)
			{
				//Conditions for starting are the mouse is over, the button is pressed, and no other items are being dragged
				if (((EOInventory) parent).NoItemsDragging())
				{
					//start the drag operation and hide the item label
					m_beingDragged = true;
					m_nameLabel.Visible = false;
				}
			}

			if (m_beingDragged && PreviousMouseState.LeftButton == ButtonState.Pressed &&
			    currentState.LeftButton == ButtonState.Pressed)
			{
				//dragging has started. continue dragging until mouse is released, update position based on mouse location
				DrawLocation = new Vector2(currentState.X - (DrawArea.Width/2) - xOff, currentState.Y - (DrawArea.Height/2) - yOff); //xOff/yOff: included in calculations later
			}
			else if (m_beingDragged && PreviousMouseState.LeftButton == ButtonState.Pressed &&
			         currentState.LeftButton == ButtonState.Released)
			{
				//need to check for: drop on map (drop action)
				//					 drop on button junk/drop
				//					 drop on grid (inventory move action)

				if (((EOInventory) parent).IsOverDrop())
				{
					//todo: if amount > 1 - ask how many to drop
					if (m_itemData.Special == ItemSpecial.Lore)
					{
						EODialog dlg = new EODialog(Game, "It is not possible to drop or trade this item.", "Lore Item", XNADialogButtons.Ok, true);
					}
					else
					{
						Handlers.Item.DropItem(m_inventory.id, 1);
					}
				}
				else if (((EOInventory) parent).IsOverJunk())
				{
					//todo: if amount > 1 - ask how many to junk
					Handlers.Item.JunkItem(m_inventory.id, 1);
				}
				/*todo: Add map drop check!*/
				
				//update the location - if it isn't on the grid, the bounds check will set it back to where it used to be originally
				//Item amount will be updated or item will be removed in packet response to the drop operation
				UpdateItemLocation(ItemCurrentSlot());

				//mouse has been released. finish dragging.
				m_beingDragged = false;
				m_nameLabel.Visible = true;
			}

			if (!m_beingDragged && PreviousMouseState.LeftButton == ButtonState.Pressed &&
			    currentState.LeftButton == ButtonState.Released && MouseOver && MouseOverPreviously)
			{
				Interlocked.Increment(ref m_recentClickCount);
				if (m_recentClickCount == 2)
				{
					_handleDoubleClick();
				}
			}

			if (!MouseOverPreviously && MouseOver && !m_beingDragged)
			{
				m_nameLabel.Visible = true;
				EOGame.Instance.Hud.SetStatusLabel("[ Item ] " + m_nameLabel.Text);
			}
			else if (!MouseOver && !m_beingDragged && m_nameLabel != null && m_nameLabel.Visible)
			{
				m_nameLabel.Visible = false;
			}

			base.Update(gameTime); //sets mouseoverpreviously = mouseover, among other things
		}

		public override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);
			if (!Visible) return;

			SpriteBatch.Begin();
			if (MouseOver)
			{
				int currentSlot = ItemCurrentSlot();
				Vector2 drawLoc = m_beingDragged
					? new Vector2(xOff + 13 + 26*(currentSlot%EOInventory.INVENTORY_ROW_LENGTH),
						yOff + 9 + 26*(currentSlot/EOInventory.INVENTORY_ROW_LENGTH)) //recalculate the top-left point for the highlight based on the current drag position
					: new Vector2(DrawAreaWithOffset.X, DrawAreaWithOffset.Y);

				if (EOInventory.GRID_AREA.Contains(new Rectangle((int) drawLoc.X, (int) drawLoc.Y, DrawArea.Width, DrawArea.Height)))
					SpriteBatch.Draw(m_highlightBG, drawLoc, Color.White);
			}
			if(m_itemgfx != null)
				SpriteBatch.Draw(m_itemgfx, new Vector2(DrawAreaWithOffset.X, DrawAreaWithOffset.Y), Color.White);
			SpriteBatch.End();
		}
		
		public void UpdateItemLocation(int newSlot)
		{
			if (Slot != newSlot && ((EOInventory) parent).MoveItem(this, newSlot)) Slot = newSlot;

			//top-left grid slot in the inventory is 115, 339
			//parent top-left is 103, 330
			//grid size is 26*26 (w/o borders 23*23)
			int width, height;
			EOInventory._getItemSizeDeltas(m_itemData.Size, out width, out height);
			DrawLocation = new Vector2(13 + 26 * (Slot % EOInventory.INVENTORY_ROW_LENGTH), 9 + 26 * (Slot / EOInventory.INVENTORY_ROW_LENGTH));
			_setSize(width * 26, height * 26);

			if (m_nameLabel != null) //fix the position of the name label too if we aren't creating the inventoryitem
			{
				m_nameLabel.DrawLocation = new Vector2(DrawArea.Width, 0);
				if(!EOInventory.GRID_AREA.Contains(m_nameLabel.DrawAreaWithOffset))
					m_nameLabel.DrawLocation = new Vector2(-m_nameLabel.DrawArea.Width, 0); //show on the right if it isn't in bounds!
				m_nameLabel.ResizeBasedOnText(16, 9);
			}
		}

		public int ItemCurrentSlot()
		{
			if (!m_beingDragged) return Slot;

			//convert the current draw area to a slot number (for when the item is dragged)
			return (int)((DrawLocation.X - 13)/26) + EOInventory.INVENTORY_ROW_LENGTH * (int)((DrawLocation.Y - 9)/26);
		}

		public void UpdateItemLabel()
		{
			switch (m_itemData.ID)
			{
				case 1: m_nameLabel.Text = string.Format("{0} {1}", m_inventory.amount, m_itemData.Name); break;
				default:
					if (m_inventory.amount == 1)
						m_nameLabel.Text = m_itemData.Name;
					else if (m_inventory.amount > 1)
						m_nameLabel.Text = string.Format("{0} x{1}", m_itemData.Name, m_inventory.amount);
					else
						throw new Exception("There shouldn't be an item in the inventory with amount zero");
					break;
			}
		}

		protected override void Dispose(bool disposing)
		{
			if(m_recentClickTimer != null) m_recentClickTimer.Dispose();
			if(m_nameLabel != null) m_nameLabel.Dispose();
			if(m_highlightBG != null) m_highlightBG.Dispose();

			base.Dispose(disposing);
		}

		private void _initItemLabel()
		{
			if (m_nameLabel != null) m_nameLabel.Dispose();

			m_nameLabel = new XNALabel(Game, new Rectangle((int)DrawLocation.X + DrawArea.Width, (int)DrawLocation.Y, 150, 23), "Microsoft Sans MS", 8f)
			{
				Visible = false,
				AutoSize = false,
				TextAlign = ContentAlignment.MiddleCenter,
				ForeColor = System.Drawing.Color.FromArgb(255, 200, 200, 200),
				BackColor = System.Drawing.Color.FromArgb(160, 30, 30, 30)
			};
			
			UpdateItemLabel();

			switch (m_itemData.Special)
			{
				case ItemSpecial.Lore:
					m_nameLabel.ForeColor = System.Drawing.Color.FromArgb(0xff, 0xff, 0xf0, 0xa5);
					break;
				//other special types have different forecolors (rare items?)
			}

			m_nameLabel.SetParent(this);
			m_nameLabel.ResizeBasedOnText(16, 9);
		}

		private void _handleDoubleClick()
		{
			string whichAction = "";
			//double-click!
			switch (m_itemData.Type) //different types of items do different things when acted on
			{
				case ItemType.Accessory:
				case ItemType.Armlet:
				case ItemType.Armor:
				case ItemType.Belt:
				case ItemType.Boots:
				case ItemType.Bracer:
				case ItemType.Gloves:
				case ItemType.Hat:
				case ItemType.Necklace:
				case ItemType.Ring:
				case ItemType.Shield:
				case ItemType.Weapon:
					Handlers.Paperdoll.EquipItem((short)m_itemData.ID);
					break;
				case ItemType.Beer:
					whichAction = "Got hella drunk on";
					break;
				case ItemType.CureCurse:
					whichAction = "Cured curse using";
					break;
				case ItemType.EXPReward:
					whichAction = "Experience reward from ";
					break;
				case ItemType.EffectPotion:
					whichAction = "Effect potion: ";
					break;
				case ItemType.HairDye:
					whichAction = "Dyed hair with";
					break;
				case ItemType.Heal:
					whichAction = "Restored health with";
					break;
				case ItemType.SkillReward:
					whichAction = "Skill reward with";
					break;
				case ItemType.StatReward:
					whichAction = "Stat reward with";
					break;
				case ItemType.Teleport:
					whichAction = "Preparing to teleport using";
					break;
			}

			if (whichAction != "")
			{
				EODialog tst = new EODialog(Game, whichAction + " item " + m_itemData.Name, "Equip action");

				if (false)
				{
					//todo: implement the 'use' action for item types
				}
			}

			m_recentClickCount = 0;
		}
	}

	public class EOInventory : XNAControl
	{
		/// <summary>
		/// number of slots in an inventory row
		/// </summary>
		public const int INVENTORY_ROW_LENGTH = 14;

		/// <summary>
		/// Area of the grid portion of the inventory (uses absolute coordinates)
		/// </summary>
		public static Rectangle GRID_AREA = new Rectangle(115, 339, 367, 106);

		private readonly bool[,] m_filledSlots = new bool[4, INVENTORY_ROW_LENGTH]; //4 rows, 14 columns = 56 total in grid
		private readonly RegistryKey m_inventoryKey;
		private readonly List<EOInventoryItem> m_childItems = new List<EOInventoryItem>();

		private readonly XNALabel m_lblWeight;
		private readonly XNAButton m_btnDrop, m_btnJunk, m_btnPaperdoll;
		
		public EOInventory(Game g)
			: base(g)
		{
			//load info from registry
			Dictionary<int, int> localItemSlotMap = new Dictionary<int, int>();
			m_inventoryKey = _tryGetCharacterRegKey();
			if (m_inventoryKey != null)
			{
				const string itemFmt = "item{0}";
				for (int i = 0; i < INVENTORY_ROW_LENGTH * 4; ++i)
				{
					int id;
					try
					{
						id = Convert.ToInt32(m_inventoryKey.GetValue(string.Format(itemFmt, i)));
					}
					catch { continue; }
					localItemSlotMap.Add(i, id);
				}
			}

			//add the inventory items that were retrieved from the server
			List<InventoryItem> localInv = World.Instance.MainPlayer.ActiveCharacter.Inventory;
			if (localInv.Find(_item => _item.id == 1).id != 1)
				localInv.Insert(0, new InventoryItem {amount = 0, id = 1}); //add 0 gold if there isn't any gold

			foreach (InventoryItem item in localInv)
			{
				ItemRecord rec = World.Instance.EIF.GetItemRecordByID(item.id);
				int slot = localItemSlotMap.ContainsValue(item.id)
					? localItemSlotMap.First(_pair => _pair.Value == item.id).Key
					: GetNextOpenSlot(rec.Size);
				if (!AddItemToSlot(slot, rec, item.amount)) throw new Exception("Too many items in inventory! (they don't fit)");
			}

			//coordinates for parent of EOInventory: 102, 330 (pnlInventory in InGameHud)
			//extra in photoshop right now: 8, 31

			//current weight label (member variable, needs to have text updated when item amounts change)
			m_lblWeight = new XNALabel(g, new Rectangle(385, 37, 88, 18), "Microsoft Sans MS", 8f)
			{
				ForeColor = System.Drawing.Color.FromArgb(255, 0xc8, 0xc8, 0xc8),
				TextAlign = ContentAlignment.MiddleCenter,
				Visible = true,
				AutoSize = false
			};
			m_lblWeight.SetParent(this);
			UpdateWeightLabel();

			Texture2D thatWeirdSheet = GFXLoader.TextureFromResource(GFXTypes.PostLoginUI, 27); //oh my gawd the offsets on this bish

			//(local variables, added to child controls)
			//'paperdoll' button
			m_btnPaperdoll = new XNAButton(g, thatWeirdSheet, new Vector2(385, 9), /*new Rectangle(39, 385, 88, 19)*/null, new Rectangle(126, 385, 88, 19));
			m_btnPaperdoll.SetParent(this);
			m_btnPaperdoll.OnClick += (s, e) => { }; //todo: make event handler that shows a paperdoll dialog
			//'drop' button
			//491, 398 -> 389, 68
			//0,15,38,37
			//0,52,38,37
			m_btnDrop = new XNAButton(g, thatWeirdSheet, new Vector2(389, 68), new Rectangle(0, 15, 38, 37), new Rectangle(0, 52, 38, 37));
			m_btnDrop.SetParent(this);
			//'junk' button - 4 + 38 on the x away from drop
			m_btnJunk = new XNAButton(g, thatWeirdSheet, new Vector2(431, 68), new Rectangle(0, 89, 38, 37), new Rectangle(0, 126, 38, 37));
			m_btnJunk.SetParent(this);
		}

		//-----------------------------------------------------
		// Overrides / Control Interface
		//-----------------------------------------------------
		public override void Update(GameTime gameTime)
		{
			if (IsOverDrop())
			{
				EOGame.Instance.Hud.SetStatusLabel("[ Button ] Drag an item to this button to drop it on the ground.");
			}
			else if (IsOverJunk())
			{
				EOGame.Instance.Hud.SetStatusLabel("[ Button ] Drag an item to this button to destroy it forever.");
			}
			else if (m_btnPaperdoll.MouseOver && !m_btnPaperdoll.MouseOverPreviously)
			{
				EOGame.Instance.Hud.SetStatusLabel("[ Button ] Click here to show your paperdoll.");
			}

			base.Update(gameTime);
		}

		protected override void Dispose(bool disposing)
		{
			m_inventoryKey.Dispose();
		}

		//-----------------------------------------------------
		// Public Access methods
		//-----------------------------------------------------
		public bool AddItemToSlot(int slot, ItemRecord item, int count = 1)
		{
			//this is ADD item - don't allow adding items that have been added already
			if (slot < 0 || m_childItems.Count(_item => _item.Slot == slot) > 0) return false;
			
			List<Tuple<int, int>> points;
			if (!_fitsInSlot(slot, item.Size, out points)) return false;
			points.ForEach(point => m_filledSlots[point.Item1, point.Item2] = true); //flag that the spaces are taken

			m_inventoryKey.SetValue(string.Format("item{0}", slot), item.ID, RegistryValueKind.String); //update the registry
			m_childItems.Add(new EOInventoryItem(Game, slot, item, new InventoryItem { amount = count, id = (short)item.ID }, this)); //add the control wrapper for the item
			m_childItems.Last().DrawOrder = (int) ControlDrawLayer.BaseLayer + 2 + (INVENTORY_ROW_LENGTH - slot%INVENTORY_ROW_LENGTH);
			children.Sort((x, y) => x.DrawOrder - y.DrawOrder);
			return true;
		}

		public void RemoveItemFromSlot(int slot, int count = 1)
		{
			EOInventoryItem control = m_childItems.Find(_control => _control.Slot == slot);
			if (control == null || slot < 0) return;

			int numLeft = control.Inventory.amount - count;

			if (numLeft <= 0)
			{
				ItemSize sz = control.ItemData.Size;
				List<Tuple<int, int>> points = _getTakenSlots(control.Slot, sz);
				points.ForEach(_p => m_filledSlots[_p.Item1, _p.Item2] = false);

				m_inventoryKey.SetValue(string.Format("item{0}", slot), 0, RegistryValueKind.String);
				m_childItems.Remove(control);
				control.Visible = false;
				control.Close();
			}
			else
				control.Inventory = new InventoryItem {amount = numLeft, id = control.Inventory.id};
		}

		public bool MoveItem(EOInventoryItem childItem, int newSlot)
		{
			if (childItem.Slot == newSlot) return true; // We did it, Reddit!

			List<Tuple<int, int>> oldPoints = _getTakenSlots(childItem.Slot, childItem.ItemData.Size);
			List<Tuple<int, int>> points;
			if (!_fitsInSlot(newSlot, childItem.ItemData.Size, out points, oldPoints)) return false;

			oldPoints.ForEach(_p => m_filledSlots[_p.Item1, _p.Item2] = false);
			points.ForEach(_p => m_filledSlots[_p.Item1, _p.Item2] = true);

			m_inventoryKey.SetValue(string.Format("item{0}", childItem.Slot), 0, RegistryValueKind.String);
			m_inventoryKey.SetValue(string.Format("item{0}", newSlot), childItem.ItemData.ID, RegistryValueKind.String);

			childItem.DrawOrder = (int)ControlDrawLayer.BaseLayer + 2 + (INVENTORY_ROW_LENGTH - childItem.Slot % INVENTORY_ROW_LENGTH);
			children.Sort((x, y) => x.DrawOrder - y.DrawOrder);

			return true;
		}

		public int GetNextOpenSlot(ItemSize size)
		{
			int width, height;
			_getItemSizeDeltas(size, out width, out height);

			//outer loops: iterating over every grid space (56 spaces total)
			for (int row = 0; row < 4; ++row)
			{
				for (int col = 0; col < INVENTORY_ROW_LENGTH; ++col)
				{
					if (m_filledSlots[row, col]) continue;

					if (!m_filledSlots[row, col] && size == ItemSize.Size1x1)
						return row*INVENTORY_ROW_LENGTH + col;

					//inner loops: iterating over grid spaces starting at (row, col) for the item size (width, height)
					bool ok = true;
					for (int y = row; y < row + height; ++y)
					{
						if (y >= 4)
						{
							ok = false;
							continue;
						}
						for (int x = col; x < col + width; ++x)
							if (x >= INVENTORY_ROW_LENGTH || m_filledSlots[y, x]) ok = false;
					}

					if (ok) return row*INVENTORY_ROW_LENGTH + col;
				}
			}

			return -1;
		}

		public void UpdateWeightLabel(string text = "")
		{
			if (string.IsNullOrEmpty(text))
				m_lblWeight.Text = string.Format("{0} / {1}", World.Instance.MainPlayer.ActiveCharacter.Weight,
					World.Instance.MainPlayer.ActiveCharacter.MaxWeight);
			else
				m_lblWeight.Text = text;
		}

		public bool NoItemsDragging()
		{
			return m_childItems.Count(invItem => invItem.Dragging) == 0;
		}

		public void UpdateItem(InventoryItem item)
		{
			EOInventoryItem ctrl;
			if((ctrl = m_childItems.Find(_ctrl => _ctrl.ItemData.ID == item.id)) != null)
			{
				ctrl.Inventory = item;
				ctrl.UpdateItemLabel();
			}
			else
			{
				ItemRecord rec = World.Instance.EIF.GetItemRecordByID(item.id);
				AddItemToSlot(GetNextOpenSlot(rec.Size), rec, item.amount);
			}
		}

		public void RemoveItem(int id)
		{
			EOInventoryItem ctrl;
			if ((ctrl = m_childItems.Find(_ctrl => _ctrl.ItemData.ID == id)) != null)
			{
				RemoveItemFromSlot(ctrl.Slot, ctrl.Inventory.amount);
			}
		}

		public bool IsOverDrop()
		{
			return m_btnDrop.MouseOver && m_btnDrop.MouseOverPreviously;
		}

		public bool IsOverJunk()
		{
			return m_btnJunk.MouseOver && m_btnJunk.MouseOverPreviously;
		}

		//-----------------------------------------------------
		// Helper methods
		//-----------------------------------------------------
// ReSharper disable PossibleNullReferenceException
		private static RegistryKey _tryGetCharacterRegKey()
		{	
			try
			{
				using (RegistryKey currentUser = Registry.CurrentUser)
				{
					using (RegistryKey software = currentUser.OpenSubKey("Software", true))
					{
						using (RegistryKey client = software.OpenSubKey("EndlessClient", true) ??
						                            software.CreateSubKey("EndlessClient", RegistryKeyPermissionCheck.ReadWriteSubTree))
						{
							using (RegistryKey eoAccount = client.OpenSubKey(World.Instance.MainPlayer.AccountName, true) ??
							                               client.CreateSubKey(World.Instance.MainPlayer.AccountName,
								                               RegistryKeyPermissionCheck.ReadWriteSubTree))
							{
								using (RegistryKey character = eoAccount.OpenSubKey(World.Instance.MainPlayer.ActiveCharacter.Name, true) ??
								       eoAccount.CreateSubKey(World.Instance.MainPlayer.ActiveCharacter.Name,
									       RegistryKeyPermissionCheck.ReadWriteSubTree))
								{
									return character.OpenSubKey("inventory", true) ??
									       character.CreateSubKey("inventory", RegistryKeyPermissionCheck.ReadWriteSubTree);
								}
							}
						}
					}
				}
			}
			catch (NullReferenceException) { }
			return null;
		}
// ReSharper restore PossibleNullReferenceException

		private List<Tuple<int, int>> _getTakenSlots(int slot, ItemSize sz)
		{
			var ret = new List<Tuple<int, int>>();

			int width, height;
			_getItemSizeDeltas(sz, out width, out height);
			int y = slot / INVENTORY_ROW_LENGTH, x = slot % INVENTORY_ROW_LENGTH;
			for (int row = y; row < height + y; ++row)
			{
				for (int col = x; col < width + x; ++col)
				{
					ret.Add(new Tuple<int, int>(row, col));
				}
			}

			return ret;
		}

		/// <summary>
		/// Returns whether or not a slot can support an item of the specified size
		/// </summary>
		/// <param name="slot">The slot to check</param>
		/// <param name="sz">The size of the item we're trying to fit</param>
		/// <param name="points">List of coordinates that the new item will take</param>
		/// <param name="itemPoints">List of coordinates of the item that is moving</param>
		/// <returns></returns>
		private bool _fitsInSlot(int slot, ItemSize sz, out List<Tuple<int, int>> points, List<Tuple<int, int>> itemPoints = null)
		{
			points = new List<Tuple<int, int>>();

			if (slot < 0 || slot >= 4*INVENTORY_ROW_LENGTH) return false;

			//check the 'filled slots' array to see if the item can go in 'slot' based on its size
			int y = slot / INVENTORY_ROW_LENGTH, x = slot % INVENTORY_ROW_LENGTH;
			if (y >= 4 || x >= INVENTORY_ROW_LENGTH) return false;
			if (itemPoints == null && m_filledSlots[y, x]) return false;

			points = _getTakenSlots(slot, sz);
			if (points.Count(_t => _t.Item1 < 0 || _t.Item1 > 3 || _t.Item2 < 0 || _t.Item2 >= INVENTORY_ROW_LENGTH) > 0)
				return false; //some of the coordinates are out of bounds of the maximum inventory length

			List<Tuple<int,int>> overLaps = points.FindAll(_pt => m_filledSlots[_pt.Item1, _pt.Item2]);
			if (overLaps.Count > 0 && (itemPoints == null || overLaps.Count(itemPoints.Contains) != overLaps.Count))
				return false; //more than one overlapping point, and the points in overLaps are not contained in itemPoints

			return true;
		}

		//this is public because C# doesn't have a 'friend' keyword and I need it in EOInventoryItem
		public static void _getItemSizeDeltas(ItemSize size, out int width, out int height)
		{
			//enum ItemSize: Size[width]x[height], 
			//	where [width] is index 4 and [height] is index 6 (string of length 7)
			string sizeStr = Enum.GetName(typeof(ItemSize), size);
			if (sizeStr == null || sizeStr.Length != 7)
			{
				width = height = 0;
				return;
			}

			width = Convert.ToInt32(sizeStr.Substring(4, 1));
			height = Convert.ToInt32(sizeStr.Substring(6, 1));
		}
	}
}
