using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;
using KillYou3000.Common.ModSystems;
using KillYou3000.MyInterface.MyItem;
using KillYou3000.Players;

namespace KillYou3000.Items.Accessories
{
    public class LifePercentAccessory : ModItem, IMiddleClickToInteractWithItems
    {
        // 饰品属性
        public int killCounter;
        public int lifeBonus;
        public int maxKillCount = 100;
        public double damagePercentage = 1;
        public double revertPercentage = 1;
        public bool isStateActive;
        public bool lighting;

        public override void SetDefaults() {
            Item.width = 28;
            Item.height = 28;
            Item.accessory = true;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.sellPrice(0, 10, 0, 0);
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.useTurn = true;
            damagePercentage = 1;
            isStateActive = true;
            lighting = true;
        }

        public override void SaveData(TagCompound tag) {
            tag["killCounter"] = killCounter;
            tag["lifeBonus"] = lifeBonus;
            tag["isStateActive"] = isStateActive;
            tag["lighting"] = lighting;
            tag["maxKillCount"] = maxKillCount;
            tag["damagePercentage"] = damagePercentage;
            tag["revertPercentage"] = revertPercentage;
        }

        public override void LoadData(TagCompound tag) {
            killCounter = tag.GetInt("killCounter");
            lifeBonus = tag.GetInt("lifeBonus");
            isStateActive = tag.GetBool("isStateActive");
            lighting = tag.GetBool("lighting");
            maxKillCount = tag.GetInt("maxKillCount");
            damagePercentage = tag.GetDouble("damagePercentage");
            revertPercentage = tag.GetDouble("revertPercentage");
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips) {
            foreach (TooltipLine line in tooltips) {
                if (line.Text.Contains("{0}")) line.Text = line.Text.Replace("{0}", killCounter.ToString());
                if (line.Text.Contains("{1}")) line.Text = line.Text.Replace("{1}", lifeBonus.ToString());
                if (line.Text.Contains("{2}"))
                    line.Text = line.Text.Replace("{2}", isStateActive ? "[c/55FF55:开启]" : "[c/FF5555:关闭]");
                if (line.Text.Contains("{3}"))
                    line.Text = line.Text.Replace("{3}", KeyBindSystem.ItemInteractions.DisplayName.Value);
                if (line.Text.Contains("{n0}")) line.Text = line.Text.Replace("{n0}", damagePercentage.ToString());
                if (line.Text.Contains("{n1}"))
                    line.Text = line.Text.Replace("{n1}", (damagePercentage / 10D).ToString());
                if (line.Text.Contains("{n2}")) line.Text = line.Text.Replace("{n2}", revertPercentage.ToString());
                if (line.Text.Contains("{n3}")) line.Text = line.Text.Replace("{n3}", maxKillCount.ToString());
            }
        }

        public void Interaction() {
            if (Main.keyState.IsKeyDown(Keys.LeftControl)) {
                LifePercentUISystem.ShowUI(this);
                return;
            }

            if (Main.keyState.IsKeyDown(Keys.LeftShift)) {
                lighting = !lighting;
                return;
            }
            
            isStateActive = !isStateActive;
        }

        public override void UpdateAccessory(Player player, bool hideVisual) {
            player.GetModPlayer<LifePercentPlayer>().hasAccessory = true;
            player.GetModPlayer<LifePercentPlayer>().accessoryList.Add(Item);
            int num = player.statLifeMax2 + lifeBonus;
            player.maxMinions += (int)(lifeBonus * 0.001f);
            if (num <= 0) player.statLifeMax2 = 0x7fffffff;
            else player.statLifeMax2 += lifeBonus;
        }

        public override void AddRecipes() {
            CreateRecipe().AddIngredient(ItemID.LifeCrystal, 5).Register();
        }
    }

    public class LifePercentUISystem : ModSystem
    {
        private static UserInterface _configInterface;
        private static LifePercentConfigUi _configUI;

        public override void Load() {
            if (!Main.dedServ) {
                _configInterface = new UserInterface();
                _configUI = new LifePercentConfigUi();
                _configUI.Activate();
                _configInterface.SetState(null);
            }
        }

        public override void UpdateUI(GameTime gameTime) {
            if (_configInterface?.CurrentState != null)
                _configInterface.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
            int mouseIndex = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
            if (mouseIndex != -1) {
                layers.Insert(mouseIndex, new LegacyGameInterfaceLayer(
                    "LifePercentConfig",
                    delegate {
                        if (_configInterface?.CurrentState != null)
                            _configInterface.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI
                ));
            }
        }

        public static void ShowUI(LifePercentAccessory item) {
            _configUI.SetItem(item);
            _configInterface.SetState(_configUI);
            Main.playerInventory = false;
            SoundEngine.PlaySound(SoundID.MenuOpen);
        }

        public static void HideUI() {
            _configInterface.SetState(null);
            Main.playerInventory = true;
        }
    }

    public class LifePercentConfigUi : UIState
    {
        private UIPanel _configPanel;
        private UiTextInput _damageInput;
        private UiTextInput _revertInput;
        private UiTextInput _maxKillInput;
        public LifePercentAccessory CurrentItem { get; set; }

        public override void OnInitialize() {
            _configPanel = new UIPanel();
            _configPanel.Width.Set(400, 0);
            _configPanel.Height.Set(250, 0);
            _configPanel.HAlign = _configPanel.VAlign = 0.5f;
            _configPanel.BackgroundColor = new Color(33, 43, 79) * 0.8f;
            Append(_configPanel);

            // 标题
            var title = new UIText("饰品配置", 0.8f);
            title.HAlign = 0.5f;
            title.Top.Set(10, 0);
            _configPanel.Append(title);

            // 配置行
            CreateConfigRow("伤害百分比:", 50, out _damageInput, value => {
                if (double.TryParse(value, out double val)) CurrentItem.damagePercentage = val;
            });
            CreateConfigRow("恢复百分比:", 90, out _revertInput, value => {
                if (double.TryParse(value, out double val)) CurrentItem.revertPercentage = val;
            });
            CreateConfigRow("最大击杀数:", 130, out _maxKillInput, value => {
                if (int.TryParse(value, out int val)) CurrentItem.maxKillCount = val;
            });

            // 关闭按钮
            var closeButton = new UIText("保存并关闭");
            closeButton.HAlign = 0.5f;
            closeButton.Top.Set(180, 0);
            closeButton.OnLeftClick += (_, __) => {
                LifePercentUISystem.HideUI();
                SoundEngine.PlaySound(SoundID.MenuClose);
            };
            _configPanel.Append(closeButton);
        }

        private void CreateConfigRow(string label, float top, out UiTextInput input, Action<string> onChange) {
            // 标签
            var labelText = new UIText(label);
            labelText.Left.Set(20, 0);
            labelText.Top.Set(top, 0);
            _configPanel.Append(labelText);

            // 输入框
            input = new UiTextInput();
            input.Width.Set(80, 0);
            input.Height.Set(30, 0);
            input.Left.Set(200, 0);
            input.Top.Set(top - 5, 0);
            input.OnTextChange += onChange;
            _configPanel.Append(input);

            // 获取输入框的本地引用
            var localInput = input;

            // 增加按钮
            var upButton = new UIText("▲");
            upButton.Left.Set(300, 0);
            upButton.Top.Set(top, 0);

            upButton.OnLeftClick += (_, __) => {
                if (double.TryParse(localInput.Text, out double val)) {
                    val++;
                    localInput.SetText(val.ToString());
                    onChange(val.ToString());
                }
            };
            _configPanel.Append(upButton);

            // 减少按钮
            var downButton = new UIText("▼");
            downButton.Left.Set(330, 0);
            downButton.Top.Set(top, 0);
            downButton.OnLeftClick += (_, __) => {
                if (double.TryParse(localInput.Text, out double val) && val > 0) {
                    val--;
                    localInput.SetText(val.ToString());
                    onChange(val.ToString());
                }
            };
            _configPanel.Append(downButton);
        }

        public void SetItem(LifePercentAccessory item) {
            CurrentItem = item;
            _damageInput.SetText(item.damagePercentage.ToString());
            _revertInput.SetText(item.revertPercentage.ToString());
            _maxKillInput.SetText(item.maxKillCount.ToString());
        }
    }

    public class UiTextInput : UIPanel
    {
        public string Text = "";
        public event Action<string> OnTextChange;
        private bool _isActive;
        private int _cursorTimer;
        private int _lastKeyPressTime;
        private string _lastKeyPress = "";

        public UiTextInput() {
            SetPadding(0);
            BackgroundColor = new Color(30, 30, 40);
            BorderColor = Color.Gray;
        }

        public override void LeftClick(UIMouseEvent evt) {
            _isActive = true;
            _cursorTimer = 0;
        }

        public void SetText(string text) {
            if (Text != text) {
                Text = text;
                OnTextChange?.Invoke(text);
            }
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);

            if (_isActive) {
                // 阻止游戏处理输入
                Main.blockInput = true;
                Main.player[Main.myPlayer].mouseInterface = true;

                // 处理键盘输入
                HandleKeyboardInput();
            }
        }

        private void HandleKeyboardInput() {
            // 处理退格键
            if (Main.keyState.IsKeyDown(Keys.Back) && !Main.oldKeyState.IsKeyDown(Keys.Back)) {
                if (Text.Length > 0) {
                    SetText(Text.Substring(0, Text.Length - 1));
                }
            }

            // foreach (var key in Enum.GetValues<Keys>()) {
            //     if (Main.keyState.IsKeyDown(key))
            //         Console.WriteLine(key);
            // }
            //
            
            // 处理小数点
            if (Main.keyState.IsKeyDown(Keys.OemPeriod) && !Main.oldKeyState.IsKeyDown(Keys.OemPeriod)) {
                SetText(Text + ".");
            }

            // 处理数字键输入
            for (int i = 0; i <= 9; i++) {
                Keys key = Keys.D0 + i;
                if (Main.keyState.IsKeyDown(key) && !Main.oldKeyState.IsKeyDown(key)) {
                    SetText(Text + i.ToString());
                }
            }

            // 处理小键盘数字键
            for (int i = 0; i <= 9; i++) {
                Keys key = Keys.NumPad0 + i;
                if (Main.keyState.IsKeyDown(key) && !Main.oldKeyState.IsKeyDown(key)) {
                    SetText(Text + i.ToString());
                }
            }

            // 处理回车键和ESC键失去焦点
            if (Main.keyState.IsKeyDown(Keys.Enter) && !Main.oldKeyState.IsKeyDown(Keys.Enter) ||
                Main.keyState.IsKeyDown(Keys.Escape) && !Main.oldKeyState.IsKeyDown(Keys.Escape)) {
                _isActive = false;
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch) {
            base.DrawSelf(spriteBatch);

            // 绘制文本
            Vector2 pos = GetDimensions().Position();
            pos.X += 10;
            pos.Y += 8;
            Utils.DrawBorderString(spriteBatch, Text, pos, Color.White, 0.8f);

            // 绘制光标
            if (_isActive) {
                _cursorTimer++;
                if (_cursorTimer % 40 < 20) {
                    if (FontAssets.MouseText != null && FontAssets.MouseText.Value != null) {
                        Vector2 textSize = FontAssets.MouseText.Value.MeasureString(Text);
                        Vector2 cursorPos = new Vector2(pos.X + textSize.X + 2, pos.Y);
                        Utils.DrawBorderString(spriteBatch, "|", cursorPos, Color.White, 0.8f);
                    }
                }

                // 检测失去焦点
                if (!IsMouseHovering && Main.mouseLeft) {
                    _isActive = false;
                }
            }
        }
    }
}