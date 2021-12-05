using UnityEngine;

namespace NonStandard {
	public partial class Lines {
		/// <param name="rectTransform">rectangle to draw on. should have RawImage (or no Renderer at all)</param>
		public static Texture2D GetRawImageTexture(RectTransform rectTransform) {
			UnityEngine.UI.RawImage rImg = rectTransform.GetComponent<UnityEngine.UI.RawImage>();
			if (rImg == null) { rImg = rectTransform.gameObject.AddComponent<UnityEngine.UI.RawImage>(); }
			if (rImg == null) { throw new System.Exception("unable to create a RawImage on " + rectTransform.name + ", does it already have another renderer?"); }
			Texture2D img = rImg.texture as Texture2D;
			if (img == null) {
				Rect r = rectTransform.rect;
				img = new Texture2D((int)r.width, (int)r.height);
				img.SetPixels32(0, 0, (int)r.width, (int)r.height, new Color32[(int)(r.width * r.height)]); // set pixels to the default color, which is clear
				rImg.texture = img;
			}
			return img;
		}

		/// <param name="rectTransform">rectangle to draw on. should have RawImage (or no Renderer at all)</param>
		/// <param name="start">(0,0) is lower left</param>
		/// <param name="end"></param>
		/// <param name="color"></param>
		public static void DrawLine(RectTransform rectTransform, Vector2 start, Vector2 end, Color color, bool apply = true) {
			DrawLine(rectTransform, (int)start.x, (int)start.y, (int)end.x, (int)end.y, color, apply);
		}

		/// <param name="rectTransform">rectangle to draw on. should have RawImage (or no Renderer at all)</param>
		/// <param name="x0">0 is left</param>
		/// <param name="y0">0 is bottom</param>
		/// <param name="x1"></param>
		/// <param name="y1"></param>
		/// <param name="col"></param>
		public static void DrawLine(RectTransform rectTransform, int x0, int y0, int x1, int y1, Color col, bool apply = true) {
			Texture2D img = GetRawImageTexture(rectTransform);
			DrawLine(img, x0, y0, x1, y1, col);
			if (apply) img.Apply();
		}
		public static void DrawAABB(RectTransform rectTransform, Vector2 p0, Vector2 p1, Color col, bool apply = true) {
			DrawAABB(rectTransform, (int)p0.x, (int)p0.y, (int)p1.x, (int)p1.y, col, apply);
		}
		public static void DrawAABB(RectTransform rectTransform, int x0, int y0, int x1, int y1, Color col, bool apply = true) {
			Texture2D img = GetRawImageTexture(rectTransform);
			DrawLine(img, x0, y0, x0, y1, col);
			DrawLine(img, x0, y1, x1, y1, col);
			DrawLine(img, x1, y0, x1, y1, col);
			DrawLine(img, x0, y0, x1, y0, col);
			if (apply) img.Apply();
		}

		/// <summary>draws an un-aliased single-pixel line on the given texture with the given color</summary>ne
		/// <param name="texture"></param>
		/// <param name="x0">0 is left</param>
		/// <param name="y0">0 is bottom</param>
		/// <param name="x1"></param>
		/// <param name="y1"></param>
		/// <param name="color"></param>
		public static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color) {
			int dy = y1 - y0;
			int dx = x1 - x0;
			int stepY, stepX;
			if (dy < 0) { dy = -dy; stepY = -1; } else { stepY = 1; }
			if (dx < 0) { dx = -dx; stepX = -1; } else { stepX = 1; }
			dy <<= 1;
			dx <<= 1;
			float fraction;
			texture.SetPixel(x0, y0, color);
			if (dx > dy) {
				fraction = dy - (dx >> 1);
				while (Mathf.Abs(x0 - x1) > 1) {
					if (fraction >= 0) {
						y0 += stepY;
						fraction -= dx;
					}
					x0 += stepX;
					fraction += dy;
					texture.SetPixel(x0, y0, color);
				}
			} else {
				fraction = dx - (dy >> 1);
				while (Mathf.Abs(y0 - y1) > 1) {
					if (fraction >= 0) {
						x0 += stepX;
						fraction -= dy;
					}
					y0 += stepY;
					fraction += dx;
					texture.SetPixel(x0, y0, color);
				}
			}
		}
	}
}