using Emu6502;

namespace Emu6502Gui
{
    // TODO: If drawing takes to much time to happen on the emulation thread, move it to another thread and then
    // send an interrupt to the CPU when drawing is complete.
    public class GraphicsChip : Device
    {
        private const int WIDTH = 320;
        private const int HEIGHT = 240;
        private const int SPRITE_COUNT = 32;
        private const int TILE_ROWS = HEIGHT / 8;
        private const int TILE_COLUMNS = WIDTH / 8;
        private const int TEXTURE_COUNT = 256;

        // Registers
        /// <summary>
        /// Selects between text, sprite and pixel modes.
        /// </summary>
        private const int MODE_REGISTER = 0;
        /// <summary>
        /// bit 0 - High to enable auto incrementing of the cursor when accessing the text register.
        /// bit 1 - High to enable VT-100-like command handling.
        /// bit 2 - High to enable auto incrementing tile horizontal/vertical registers as tile texture reg is written.
        /// bit 3 - High to enable auto incrementing texture offset/index registers as texture color reg is written.
        /// </summary>
        private const int CONTROL_REG = 1;
        /// <summary>
        /// Writes the character to the current cursor position.
        /// Optionally increments the current cursor position.
        /// </summary>
        private const int TEXT_REG = 4;
        /// <summary>
        /// Used to select which sprite is currently being modified by other sprite registers.
        /// </summary>
        private const int SPRITE_INDEX_REG = 5;
        /// <summary>
        /// Used to read or write the low byte of the x position of the currently selected sprite.
        /// </summary>
        private const int SPRITE_X_LO_REG = 6;
        /// <summary>
        /// Used to read or write the high byte of the x position of the currently selected sprite.
        /// </summary>
        private const int SPRITE_X_HI_REG = 7;
        /// <summary>
        /// Used to read or write the y position of the currently selected sprite.
        /// </summary>
        private const int SPRITE_Y_REG = 8;
        /// <summary>
        /// Used to read or write the texture index of the currently selected sprite.
        /// </summary>
        private const int SPRITE_TEXTURE_REG = 9;
        /// <summary>
        /// Used to select which tile is currently being modified by other tile registers.
        /// </summary>
        private const int TILE_X_REG = 10;
        /// <summary>
        /// Used to select which tile is currently being modified by other tile registers.
        /// </summary>
        private const int TILE_Y_REG = 11;
        /// <summary>
        /// Used to read or write the texture index of the currently selected tile.
        /// Accesses can optionally auto increment the y and x registers.
        /// See CONTROL register to enable/disable auto increment.
        /// </summary>
        private const int TILE_TEXTURE_REG = 12;
        /// <summary>
        /// Used to select which texture is currently being modified by other texture registers.
        /// </summary>
        private const int TEXTURE_INDEX_REG = 13;
        /// <summary>
        /// Used to select which byte of the currently selected texture's color data is being modified by the color reg.
        /// Is reset to 0 when texture index reg is accessed.
        /// </summary>
        private const int TEXTURE_OFFSET_REG = 14;
        /// <summary>
        /// Used to read or write the currently selected texture's color data.
        /// Accesses to this register can optionally auto increment the texture offset and texture index registers.
        /// </summary>
        private const int TEXTURE_COLOR_REG = 15;

        private readonly Graphics output;
        private readonly Sprite[] sprites;
        private readonly Tile[,] tiles;
        private readonly Texture[] textures;
        // TODO: Don't use nullable values
        private readonly Action<IDeviceInterface>[] regHandlers;

        public override int Length => regHandlers.Length;

        /// <summary>
        /// The index of the sprite that is currently being modified by the sprite registers.
        /// </summary>
        private byte spriteIndex = 0;
        /// <summary>
        /// The index of the texture that is currently being modified by the texture registers.
        /// </summary>
        private byte textureIndex = 0;
        /// <summary>
        /// The index into the color data of the texture that is currently being modified by the texture registers.
        /// </summary>
        private byte textureOffset = 0;
        private byte tileX = 0;
        private byte tileY = 0;
        private byte control = 0;

        private bool AutoIncTileIndex
        {
            get => (control & 0b100) != 0;
            set => control = (byte)((control & (~0b100)) | (value ? 0b100 : 0));
        }

        private bool AutoIncTextureIndex
        {
            get => (control & 0b1000) != 0;
            set => control = (byte)((control & (~0b1000)) | (value ? 0b1000 : 0));
        }

        public GraphicsChip(ushort baseAddress, Graphics graphics) : base(baseAddress)
        {
            this.output = graphics;

            sprites = new Sprite[SPRITE_COUNT];
            for (int i = 0; i < sprites.Length; i++)
                sprites[i] = new Sprite();

            tiles = new Tile[TILE_COLUMNS, TILE_ROWS];
            for (int y = 0; y < TILE_ROWS; y++)
                for (int x = 0; x < TILE_COLUMNS; x++)
                    tiles[x, y] = new Tile();

            textures = new Texture[TEXTURE_COUNT];
            for (int i = 0; i < textures.Length; i++)
                textures[i] = new Texture();

            regHandlers =
            [
                ModeReg, ControlReg, (_) => { }, (_) => { },
                TextReg, SpriteIndexReg, SpriteXLoReg, SpriteXHiReg,
                SpriteYReg, SpriteTextureReg, TileXReg, TileYReg,
                TileTextureReg, TextureIndexReg, TextureOffsetReg, TextureColorReg
            ];
        }

        public override void OnCycle(IDeviceInterface bc)
        {
            if (!InRange(bc.Address))
                return;

            ushort reg = Relative(bc.Address);
            regHandlers[reg](bc);
        }

        private void RemoveSprite(Sprite sprite)
        {
            int tileX = sprite.x / 8;
            int tileY = sprite.y / 8;
            bool twoX = sprite.x % 8 != 0;
            bool twoY = sprite.y % 8 != 0;

            DrawTile(tileX, tileY);
            if (twoX)
                DrawTile(tileX + 1, tileY);
            if (twoY)
                DrawTile(tileX, tileY + 1);
            if (twoX && twoY)
                DrawTile(tileX + 1, tileY + 1);

            // TODO: Redraw sprites that still occupy the redrawn tiles. Right now redrawing all but the removed one.
            foreach (Sprite s in sprites)
            {
                if (s == sprite)
                    continue;

                DrawSprite(s);
            }
        }

        private void DrawSprite(Sprite sprite)
        {
            Texture texture = textures[sprite.textureIndex];
            output.DrawImage(texture.bitmap, sprite.x, sprite.y);
        }

        private void DrawTile(int tileX, int tileY)
        {
            Texture texture = textures[tiles[tileX, tileY].textureIndex];
            output.DrawImage(texture.bitmap, tileX * 8, tileY * 8);
        }

        #region Register Handlers
        private void ModeReg(IDeviceInterface bc)
        {
            // TODO: Redraw all is temp functionality
            for (int y = 0; y < TILE_ROWS; y++)
                for (int x = 0; x < TILE_COLUMNS; x++)
                    DrawTile(x, y);

            for (int i = 0; i < sprites.Length; i++)
                DrawSprite(sprites[i]);
        }

        private void ControlReg(IDeviceInterface bc)
        {
            if (bc.Rwb)
                bc.Data = control;
            else
                control = bc.Data;
        }

        private void TextReg(IDeviceInterface bc)
        {

        }

        private void SpriteIndexReg(IDeviceInterface bc)
        {
            if (bc.Rwb)
                bc.Data = spriteIndex;
            else if (bc.Data < SPRITE_COUNT)
                spriteIndex = bc.Data;
        }
        
        private void SpriteXLoReg(IDeviceInterface bc)
        {
            if (bc.Rwb)
                bc.Data = sprites[spriteIndex].XLo;
            else
            {
                RemoveSprite(sprites[spriteIndex]);
                sprites[spriteIndex].XLo = bc.Data;
                DrawSprite(sprites[spriteIndex]);
            }
        }

        private void SpriteXHiReg(IDeviceInterface bc)
        {
            if (bc.Rwb)
                bc.Data = sprites[spriteIndex].XHi;
            else
            {
                RemoveSprite(sprites[spriteIndex]);
                sprites[spriteIndex].XHi = bc.Data;
                DrawSprite(sprites[spriteIndex]);
            }
        }

        private void SpriteYReg(IDeviceInterface bc)
        {
            if (bc.Rwb)
                bc.Data = sprites[spriteIndex].y;
            else
            {
                RemoveSprite(sprites[spriteIndex]);
                sprites[spriteIndex].y = bc.Data;
                DrawSprite(sprites[spriteIndex]);
            }
        }

        private void SpriteTextureReg(IDeviceInterface bc)
        {
            if (bc.Rwb)
                bc.Data = sprites[spriteIndex].textureIndex;
            else
            {
                sprites[spriteIndex].textureIndex = bc.Data;
                RemoveSprite(sprites[spriteIndex]);
                DrawSprite(sprites[spriteIndex]);
            }
        }

        private void TileXReg(IDeviceInterface bc)
        {
            if (bc.Rwb)
                bc.Data = tileX;
            else if (bc.Data < TILE_COLUMNS)
                tileX = bc.Data;
        }

        private void TileYReg(IDeviceInterface bc)
        {
            if (bc.Rwb)
                bc.Data = tileY;
            else if(bc.Data < TILE_ROWS)
                tileY = bc.Data;
        }

        private void TileTextureReg(IDeviceInterface bc)
        {
            if (bc.Rwb)
                bc.Data = tiles[tileX, tileY].textureIndex;
            else
            {
                tiles[tileX, tileY].textureIndex = bc.Data;
                DrawTile(tileX, tileY);
                // TODO: Redraw sprites over the tile. Right now just drawing all sprites.
                foreach (Sprite sprite in sprites)
                    DrawSprite(sprite);

                if (AutoIncTileIndex)
                {
                    tileX++;
                    if ((tileX %= TILE_COLUMNS) == 0)
                    {
                        tileY++;
                        tileY %= TILE_ROWS;
                    }
                }
            }
        }

        private void TextureIndexReg(IDeviceInterface bc)
        {
            if (bc.Rwb)
                bc.Data = textureIndex;
            else
                textureIndex = bc.Data;

            textureOffset = 0;
        }

        private void TextureOffsetReg(IDeviceInterface bc)
        {
            if (bc.Rwb)
                bc.Data = textureOffset;
            else if (bc.Data < Texture.TEXTURE_COLOR_DATA_SIZE)
                textureOffset = bc.Data;
        }

        private void TextureColorReg(IDeviceInterface bc)
        {
            if (bc.Rwb)
                bc.Data = textures[textureIndex].GetPixels(textureOffset);
            else
            {
                textures[textureIndex].SetPixels(textureOffset, bc.Data);

                // TODO: Redraw
                //for (int y = 0; y < TILE_ROWS; y++)
                //    for (int x = 0; x < TILE_COLUMNS; x++)
                //        if (tiles[x, y].textureIndex == textureIndex)
                //            DrawTile(x, y);

                //for (int i = 0; i < sprites.Length; i++)
                //    DrawSprite(sprites[i]);

                if (AutoIncTextureIndex)
                {
                    textureOffset++;
                    if ((textureOffset %= Texture.TEXTURE_COLOR_DATA_SIZE) == 0)
                        textureIndex++;
                }
            }
        }
        #endregion Register Handlers

        private class Sprite
        {
            public ushort x;
            public byte y;
            public byte textureIndex;

            public byte XLo
            {
                get => (byte)x;
                set => x = (ushort)((x & 0xFF00) | value);
            }

            public byte XHi
            {
                get => (byte)(x >> 8);
                set => x = (ushort)((x & 0x00FF) | (value << 8));
            }

            public Sprite()
            {
                x = 0;
                y = 0;
                textureIndex = 0;
            }
        }

        private class Tile
        {
            public byte textureIndex;

            public Tile()
            {
                textureIndex = 0;
            }
        }

        private class Texture
        {
            public const byte TEXTURE_COLOR_DATA_SIZE = 64 / 2; // 8 * 8 = 64 pixels; 64 pixels / 2 pixels per byte = 32 bytes

            // TODO: Make color pallete modifiable by CPU?
            private static readonly Color[] PALETTE =
                [
                // TODO: Select colors.
                Color.Black,
                Color.Red,
                Color.Green,
                Color.Blue,
                Color.Red,
                Color.Green,
                Color.Blue,
                Color.White,
                Color.Black,
                Color.Red,
                Color.Green,
                Color.Blue,
                Color.Red,
                Color.Green,
                Color.Blue,
                Color.White
                ];

            private static readonly Dictionary<Color, byte> PALETTE_REVERSE = new Dictionary<Color, byte>()
            {
                { Color.FromArgb(0, 0, 0), 0 },
                { Color.FromArgb(0, 0, 255), 1 },
                { Color.FromArgb(0, 255, 0), 2 },
                { Color.FromArgb(0, 255, 255), 3 },
                { Color.FromArgb(255, 0, 0), 4 },
                { Color.FromArgb(255, 0, 255), 5 },
                { Color.FromArgb(255, 255, 0), 6 },
                //{ Color.FromArgb(255, 255, 255), 7 }, // Should probably be the color used as transparency.

                { Color.FromArgb(127, 127, 127), 8 },
                { Color.FromArgb(127, 127, 255), 9 },
                { Color.FromArgb(127, 255, 127), 10 },
                { Color.FromArgb(127, 255, 255), 11 },
                { Color.FromArgb(255, 127, 127), 12 },
                { Color.FromArgb(255, 127, 255), 13 },
                { Color.FromArgb(255, 255, 127), 14 },
                { Color.FromArgb(255, 255, 255), 15 }

            };

            // Every 4 bits is a pixel's color value. The very first pixel is the low nibble of the first byte,
            // the second pixel is the high nibble of the first byte, the third is the low nibble of the second byte...
            public readonly Bitmap bitmap;

            public Texture()
            {
                bitmap = new Bitmap(8, 8);
            }

            public void SetPixels(int offset, byte values)
            {
                int x = offset % 4 * 2;
                int y = offset / 4;

                bitmap.SetPixel(x, y, PALETTE[values & 0b1111]);
                bitmap.SetPixel(x + 1, y, PALETTE[values >> 4]);
            }

            public byte GetPixels(int offset)
            {
                int x = offset % 4 * 2;
                int y = offset / 4;

                return (byte)(PALETTE_REVERSE[bitmap.GetPixel(x, y)] | (PALETTE_REVERSE[bitmap.GetPixel(x + 1, y)] << 4));
            }
        }
    }
}
