#define PULSE_COLOR
using System;
using UnityEngine;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

// author: mvaganov@hotmail.com
// license: Copyfree, public domain. This is free code! Great artists, steal this code!
// latest version at: https://pastebin.com/raw/8m69iTut -- last updated (2021/11/19)
// TODO splines? where did the Spline code go?
namespace NonStandard {
	/// <summary>static functions for Unity's LineRenderer. Creates visualizations for 3D Vector math.
	/// This library isn't optimized for performance, it's built to make math less invisible, even at compiled runtime.
	/// </summary>
	public partial class Lines : MonoBehaviour {
		/// <summary>
		/// the ends of a line.
		/// Normal is a simple rectangular end
		/// Arrow ends in an arrow head
		/// ArrowBothEnds starts and ends with an arrow head
		/// </summary>
		public enum End { Normal, Arrow, ArrowBothEnds };

		public bool autoParentLinesToGlobalObject = true;

		/// <summary>the dictionary of named lines. This structure allows Lines to create new lines without needing explicit variables</summary>
		private static readonly Dictionary<string, GameObject> NamedObject = new Dictionary<string, GameObject>();
		/// <summary>The singleton instance.</summary>
		private static Lines _instance;

		public const float ARROW_SIZE = 3, LINE_SIZE = 1f / 8, SAME_AS_START_SIZE = -1;

		public static Material _defaultMaterial;
		public static Material DefaultMaterial {
			get {
				if (_defaultMaterial != null) return _defaultMaterial;
				GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Plane);
				_defaultMaterial = primitive.GetComponent<MeshRenderer>().sharedMaterial;
				DestroyImmediate(primitive);
				return _defaultMaterial;
			}
		}

		[Tooltip("Used to draw lines. Ideally a white Sprites/Default shader."), SerializeField]
		private Material lineMaterial;
		public static Material LineMaterial {
			get {
				Lines lines = Instance;
				if (lines.lineMaterial != null) return lines.lineMaterial;
				const string colorShaderName = "Sprites/Default";//"Unlit/Color";
				lines.lineMaterial = FindShaderMaterial(colorShaderName);
				return lines.lineMaterial;
			}
		}

		public static Lines Instance {
			get {
				if (_instance) return _instance;
				return _instance = FindComponentInstance<Lines>();
			}
		}

		public static T FindComponentInstance<T>() where T : Component {
			T instance;
			if ((instance = FindObjectOfType(typeof(T)) as T) != null) return instance;
			GameObject g = new GameObject($"<{typeof(T).Name}>");
			instance = g.AddComponent<T>();
			return instance;
		}

		private void Start() {
			if (_instance == null || _instance == this) return;
			Debug.LogWarning("<Lines> should be a singleton. Deleting extra");
			Destroy(this);
		}

		/// <param name="name"></param>
		/// <param name="createIfNotFound">if true, this function will not return null</param>
		/// <returns>a line object with the given name. can return null if no such object has been made yet with this function</returns>
		public static GameObject Get(string name, bool createIfNotFound = false) {
			if ((NamedObject.TryGetValue(name, out GameObject go) && go) || !createIfNotFound) return go;
			go = NamedObject[name] = MakeLineRenderer(ref go).gameObject;
			go.name = name;
			return go;
		}

		/// <summary></summary>
		/// <returns>an unnamed, unmanaged Line object</returns>
		public static Wire MakeWire(string wirename = null) {
			GameObject go = null;
			MakeLineRenderer(ref go);
			if (!string.IsNullOrEmpty(wirename)) { go.name = wirename; }
			Wire line = go.GetComponent<Wire>();
			if (!line) { line = go.AddComponent<Wire>(); line.RefreshSource(); }
			go.layer = LayerMask.NameToLayer("UI");
			return line;
		}

		/// <summary>looks for a line object with the given name and returns it</summary>
		/// <param name="name"></param>
		/// <param name="createIfNotFound"></param>
		/// <returns>a line object with the given name, or null if createIfNotFound is false and the object doesn't exist</returns>
		public static Wire Make(string name, bool createIfNotFound = true) {
			GameObject go = Get(name, createIfNotFound);
			if (go == null) return null;
			Wire line = go.GetComponent<Wire>();
			if (!line) { line = go.AddComponent<Wire>(); line.RefreshSource(); }
			return line;
		}

		/// <summary>
		/// Make the specified Line.
		/// example usage:
		/// <para><code>
		/// /* GameObject forwardLine should be a member variable */
		/// Lines.Make (ref forwardLine, transform.position,
		///             transform.position + transform.forward, Color.blue, 0.1f, 0);
		/// //This makes a long thin triangle, pointing forward.
		/// </code></para>
		/// </summary>
		/// <param name="lineObject">GameObject host of the LineRenderer</param>
		/// <param name="start">Start, an absolute world-space coordinate</param>
		/// <param name="end">End, an absolute world-space coordinate</param>
		/// <param name="color"></param>
		/// <param name="startSize">How wide the line is at the start</param>
		/// <param name="endSize">How wide the line is at the end</param>
		public static LineRenderer Make(ref GameObject lineObject, Vector3 start, Vector3 end,
			Color color = default, float startSize = LINE_SIZE, float endSize = SAME_AS_START_SIZE) {
			LineRenderer lr = MakeLineRenderer(ref lineObject);
			SetLine(lr, color, startSize, endSize);
			lr.positionCount = 2;
			lr.SetPosition(0, start); lr.SetPosition(1, end);
			return lr;
		}

		/// <summary>Make the specified Line from a list of points</summary>
		/// <returns>The LineRenderer hosting the line</returns>
		/// <param name="lineObject">GameObject host of the LineRenderer</param>
		/// <param name="color">Color of the line</param>
		/// <param name="points">List of absolute world-space coordinates</param>
		/// <param name="pointCount">Number of the points used points list</param>
		/// <param name="startSize">How wide the line is at the start</param>
		/// <param name="endSize">How wide the line is at the end</param>
		public static LineRenderer Make(ref GameObject lineObject, IList<Vector3> points, int pointCount,
			Color color = default, float startSize = LINE_SIZE, float endSize = SAME_AS_START_SIZE) {
			LineRenderer lr = MakeLineRenderer(ref lineObject);
			return Make(lr, points, pointCount, color, startSize, endSize);
		}

		public static LineRenderer Make(LineRenderer lr, IList<Vector3> points, int pointCount,
			Color color = default, float startSize = LINE_SIZE, float endSize = SAME_AS_START_SIZE) {
			SetLine(lr, color, startSize, endSize);
			return MakeLine(lr, points, pointCount);
		}

		public static LineRenderer MakeLine(LineRenderer lr, IList<Vector3> points, int pointCount) {
			lr.positionCount = pointCount;
			for (int i = 0; i < pointCount; ++i) { lr.SetPosition(i, points[i]); }
			return lr;
		}

		public static LineRenderer MakeLine(LineRenderer lr, IList<Vector3> points, Color color, float startSize, float endSize, End lineEnds) {
			if (LinesMath.EQ2(endSize, SAME_AS_START_SIZE)) { endSize = startSize; }
			if (points == null) { lr = Make(lr, null, 0, color, startSize, endSize); return lr; }
			if (lineEnds == End.Arrow || lineEnds == End.ArrowBothEnds) {
				Keyframe[] keyframes = CalculateArrowKeyframes(points, points.Count, out var line, startSize, endSize);
				lr = MakeArrow(lr, line, line.Length, color, startSize, endSize);
				lr.widthCurve = new AnimationCurve(keyframes);
				if (lineEnds == End.ArrowBothEnds) {
					ReverseLineInternal(ref lr);
					Vector3[] p = new Vector3[lr.positionCount];
					lr.GetPositions(p);
					lr = MakeArrow(lr, p, p.Length, color, endSize, startSize, ARROW_SIZE, lr.widthCurve.keys);
					ReverseLineInternal(ref lr);
				}
			} else {
				lr = Make(lr, points, points.Count, color, startSize, endSize);
				FlattenKeyFrame(lr);
			}
			if (lr.loop && lineEnds != End.Normal) { lr.loop = false; }
			return lr;
		}

		public static void FlattenKeyFrame(LineRenderer lr) {
			AnimationCurve widthCurve = lr.widthCurve;
			Keyframe[] keys = widthCurve.keys;
			if (keys != null && keys.Length > 2) {
				lr.widthCurve = new AnimationCurve(new Keyframe[] { keys[0], keys[keys.Length - 1] });
			}
		}

		public static LineRenderer MakeLineRenderer(ref GameObject lineObject) {
			if (!lineObject) {
				lineObject = new GameObject();
				if (Instance.autoParentLinesToGlobalObject) {
					lineObject.transform.SetParent(_instance.transform);
				}
			}
			return MakeLineRenderer(lineObject);
		}

		public static LineRenderer MakeLineRenderer(GameObject lineObject) {
			LineRenderer lr = lineObject.GetComponent<LineRenderer>();
			if (!lr) { lr = lineObject.AddComponent<LineRenderer>(); }
			return lr;
		}

		public static LineRenderer SetLine(LineRenderer lr, Color color, float startSize, float endSize) {
			lr.startWidth = startSize;
			if (LinesMath.EQ2(endSize, SAME_AS_START_SIZE)) { endSize = startSize; }
			lr.endWidth = endSize;
			SetColor(lr, color);
			return lr;
		}

		public static Material FindShaderMaterial(string shaderName) {
			Shader s = Shader.Find(shaderName);
			if (!s) {
				throw new Exception("Missing shader: " + shaderName
					+ ". Please make sure it is in the \"Resources\" folder, "
					+ "or used by at least one other object in the scene. Or, "
					+ " manually assign the line material to a Lines GameObject.");
			}
			return new Material(s);
		}

		public static void SetColor(LineRenderer lr, Color color) {
			bool needsMaterial = lr.material == null || lr.material.name.StartsWith("Default-Material");
			if (needsMaterial) { lr.material = LineMaterial; }
			if (color == default) { color = Color.magenta; }
#if PULSE_COLOR
			long t = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
			long duration = 500;
			long secComponent = t % duration;
			float a = Mathf.Abs((2f * secComponent - duration) / duration);
			Color.RGBToHSV(color, out float h, out float s, out float v);
			color = Color.HSVToRGB(h, s + (a * .25f), v + (a * .25f));
#endif
			lr.material.color = color;
		}

		/// <summary>Makes a circle with a 3D line</summary>
		/// <returns>The LineRenderer hosting the line</returns>
		/// <param name="lineObj">GameObject host of the LineRenderer</param>
		/// <param name="color">Color of the line</param>
		/// <param name="center">Absolute world-space 3D coordinate</param>
		/// <param name="normal">Which way the circle is facing</param>
		/// <param name="radius"></param>
		/// <param name="pointCount">How many points to use for the circle. If zero, will do 24*PI*r</param>
		/// <param name="lineSize">The width of the line</param>
		public static LineRenderer MakeCircle(ref GameObject lineObj, Vector3 center, Vector3 normal,
			Color color = default, float radius = 1, int pointCount = 0, float lineSize = LINE_SIZE) {
			Vector3[] points = null;
			LinesMath.WriteCircle(ref points, center, normal, radius, pointCount);
			LineRenderer lr = Lines.Make(ref lineObj, points, points.Length, color, lineSize, lineSize);
			lr.loop = true;
			return lr;
		}

		public static LineRenderer MakeSphere(string name, float radius = 1,
			Vector3 center = default, Color color = default, float lineSize = LINE_SIZE) {
			GameObject go = Get(name, true);
			return MakeSphere(ref go, radius, center, color, lineSize);
		}
		/// <returns>a line renderer in the shape of a sphere made of 3 circles, for the x.y.z axis</returns>
		/// <param name="lineObj">Line object.</param>
		/// <param name="radius">Radius.</param>
		/// <param name="center">Center.</param>
		/// <param name="color">Color.</param>
		/// <param name="lineSize">Line size.</param>
		public static LineRenderer MakeSphere(ref GameObject lineObj, float radius = 1,
			Vector3 center = default, Color color = default, float lineSize = LINE_SIZE) {
			Vector3[] circles = new Vector3[24 * 3];
			LinesMath.WriteArc(ref circles, 24, Vector3.forward, Vector3.up, 360, center, 24 * 0);
			LinesMath.WriteArc(ref circles, 24, Vector3.right, Vector3.up, 360, center, 24 * 1);
			LinesMath.WriteArc(ref circles, 24, Vector3.up, Vector3.forward, 360, center, 24 * 2);
			if (LinesMath.EQ2(radius, 1)) { for (int i = 0; i < circles.Length; ++i) { circles[i] *= radius; } }
			return Lines.Make(ref lineObj, circles, circles.Length, color, lineSize, lineSize);
		}

		public static LineRenderer MakeBox(ref GameObject lineObj, Vector3 center,
			Vector3 size, Quaternion rotation, Color color = default, float lineSize = LINE_SIZE) {
			Vector3 y = Vector3.up / 2 * size.y;
			Vector3 x = Vector3.right / 2 * size.x;
			Vector3 z = Vector3.forward / 2 * size.z;
			Vector3[] line = new Vector3[] {
				 z+y-x, -z+y-x, -z-y-x, -z-y+x, -z+y+x,  z+y+x,  z-y+x,  z-y-x,
				 z+y-x,  z+y+x,  z-y+x, -z-y+x, -z+y+x, -z+y-x, -z-y-x,  z-y-x
			};
			for (int i = 0; i < line.Length; ++i) { line[i] = rotation * line[i] + center; }
			LineRenderer lr = Make(ref lineObj, line, line.Length, color, lineSize, lineSize);
			lr.numCornerVertices = 4;
			return lr;
		}

		public static LineRenderer MakeMapPin(string name, Color c = default, float size = 1, float lineSize = LINE_SIZE) {
			GameObject go = Get(name, true);
			return MakeMapPin(ref go, c, size, lineSize);
		}
		private static Vector3[] _mapPinPointsBase;
		/// <summary>Draws a "map pin", which shows a visualization for direction and orientation</summary>
		/// <returns>The LineRenderer hosting the map pin line. The LineRenderer's transform can be adjusted!</returns>
		/// <param name="lineObj">Line object.</param>
		/// <param name="c">C: color</param>
		/// <param name="size">Size: radius of the map pin</param>
		/// <param name="lineSize">Line width.</param>
		public static LineRenderer MakeMapPin(ref GameObject lineObj, Color c = default, float size = 1, float lineSize = LINE_SIZE) {
			const float epsilon = 1 / 1024.0f;
			if (_mapPinPointsBase == null) {
				Vector3 pos = Vector3.zero, forward = Vector3.forward * size, right = Vector3.right * size, up = Vector3.up;
				const float startAngle = (360.0f / 4) - (360.0f / 32);
				Vector3 v = Quaternion.AngleAxis(startAngle, up) * forward;
				LinesMath.WriteArc(ref _mapPinPointsBase, 32, up, v, 360, pos);
				Vector3 tip = pos + forward * Mathf.Sqrt(2);
				_mapPinPointsBase[0] = _mapPinPointsBase[_mapPinPointsBase.Length - 1];
				int m = (32 * 5 / 8);
				_mapPinPointsBase[++m] = _mapPinPointsBase[m] + (tip - _mapPinPointsBase[m]) * (1 - epsilon);
				_mapPinPointsBase[++m] = tip;
				int n = (32 * 7 / 8) + 1;
				while (n < 32) { _mapPinPointsBase[++m] = _mapPinPointsBase[n++]; }
				Vector3 side = pos + right;
				_mapPinPointsBase[++m] = _mapPinPointsBase[m] + (side - _mapPinPointsBase[m]) * (1 - epsilon);
				_mapPinPointsBase[++m] = pos + right;
				_mapPinPointsBase[++m] = pos + right * epsilon;
				_mapPinPointsBase[++m] = pos;
				_mapPinPointsBase[++m] = pos + up * (size * (1 - epsilon));
				_mapPinPointsBase[++m] = pos + up * size;
			}
			LineRenderer lr = Lines.Make(ref lineObj, _mapPinPointsBase, _mapPinPointsBase.Length, c, lineSize, lineSize);
			lr.useWorldSpace = false;
			return lr;
		}

		public static LineRenderer SetMapPin(string name, Transform t, Color c = default, float size = 1, float lineWidth = LINE_SIZE) {
			GameObject go = Get(name, true);
			return SetMapPin(ref go, t, c, size, lineWidth);
		}
		/// <summary>Draws a "map pin", which shows a visualization for direction and orientation</summary>
		/// <returns>The LineRenderer hosting the map pin line</returns>
		/// <param name="lineObj">Line object.</param>
		/// <param name="t">t: the transform to attach the map pin visualisation to</param>
		/// <param name="c">C: color</param>
		/// <param name="size">Size: radius of the map pin</param>
		/// <param name="lineWidth">Line width.</param>
		public static LineRenderer SetMapPin(ref GameObject lineObj, Transform t, Color c = default, float size = 1, float lineWidth = LINE_SIZE) {
			LineRenderer line_ = MakeMapPin(ref lineObj, c, size, lineWidth);
			Transform transform = line_.transform;
			transform.SetParent(t);
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			return line_;
		}


		/// <returns>a line renderer in the shape of a spiraling sphere, spiraling about the Vector3.up axis</returns>
		/// <param name="lineObj">Line object.</param>
		/// <param name="radius">Radius.</param>
		/// <param name="center">Center.</param>
		/// <param name="rotation"></param>
		/// <param name="color">Color.</param>
		/// <param name="lineSize">LineSize.</param>
		public static LineRenderer MakeSpiralSphere(ref GameObject lineObj, float radius = 1,
			Vector3 center = default, Quaternion rotation = default, Color color = default, float lineSize = LINE_SIZE) {
			Vector3[] vertices = LinesMath.CreateSpiralSphere(center, radius, rotation, 24, 3);
			return Make(ref lineObj, vertices, vertices.Length, color, lineSize, lineSize);
		}

		public static LineRenderer MakeArrow(ref GameObject lineObject, Vector3 start, Vector3 end,
			Color color = default, float startSize = LINE_SIZE, float endSize = SAME_AS_START_SIZE, float arrowHeadSize = ARROW_SIZE) {
			return MakeArrow(ref lineObject, new Vector3[] { start, end }, 2, color, startSize, endSize, arrowHeadSize);
		}

		public static LineRenderer MakeArrow(ref GameObject lineObject, IList<Vector3> points, int pointCount,
			Color color = default, float startSize = LINE_SIZE, float endSize = SAME_AS_START_SIZE,
			float arrowHeadSize = ARROW_SIZE, Keyframe[] lineKeyFrames = null) {
			LineRenderer lr = MakeLineRenderer(ref lineObject);
			return MakeArrow(lr, points, pointCount, color, startSize, endSize, arrowHeadSize, lineKeyFrames);
		}

		public static LineRenderer MakeArrow(LineRenderer lr, IList<Vector3> points, int pointCount,
				Color color = default, float startSize = LINE_SIZE, float endSize = SAME_AS_START_SIZE,
				float arrowHeadSize = ARROW_SIZE, Keyframe[] lineKeyFrames = null) {
			Keyframe[] keyframes = CalculateArrowKeyframes(points, pointCount, out Vector3[] line, startSize, endSize, arrowHeadSize, lineKeyFrames);
			Make(lr, line, line.Length, color, startSize, endSize);
			lr.widthCurve = new AnimationCurve(keyframes);
			return lr;
		}

		public static Keyframe[] CalculateArrowKeyframes(IList<Vector3> points, int pointCount, out Vector3[] line,
		float startSize = LINE_SIZE, float endSize = SAME_AS_START_SIZE, float arrowHeadSize = ARROW_SIZE, Keyframe[] lineKeyFrames = null) {
			float arrowSize = endSize * arrowHeadSize;
			int lastGoodIndex = 0;
			Vector3 arrowheadBase = Vector3.zero;
			const float distanceBetweenArrowBaseAndWidePoint = 1.0f / 512;
			Vector3 delta, dir = Vector3.zero;
			// find where, in the list of points, to place the arrowhead
			float dist = 0;
			int lastPoint = pointCount - 1;
			for (int i = lastPoint; i > 0; --i) { // go backwards (from the pointy end)
				float d = Vector3.Distance(points[i], points[i - 1]);
				dist += d;
				// if the arrow direction hasn't been calculated and sufficient distance for the arrowhead has been passed
				if (dir == Vector3.zero && dist >= arrowSize) {
					// calculate w,here the arrowheadBase should be (requires 2 points) based on the direction of this segment
					lastGoodIndex = i - 1;
					delta = points[i] - points[i - 1];
					dir = delta.normalized;
					float extraFromLastGoodIndex = dist - arrowSize;
					arrowheadBase = points[lastGoodIndex] + dir * extraFromLastGoodIndex;
				}
			}
			// if the line is not long enough for an arrow head, make the whole thing an arrowhead
			if (dist <= arrowSize) {
				line = new Vector3[] { points[0], points[lastPoint] };
				return new Keyframe[] { new Keyframe(0, arrowSize), new Keyframe(1, 0) };
			}
			delta = points[lastPoint] - arrowheadBase;
			dir = delta.normalized;
			Vector3 arrowheadWidest = arrowheadBase + dir * (dist * distanceBetweenArrowBaseAndWidePoint);
			line = new Vector3[lastGoodIndex + 4];
			for (int i = 0; i <= lastGoodIndex; i++) {
				line[i] = points[i];
			}
			line[lastGoodIndex + 3] = points[lastPoint];
			line[lastGoodIndex + 2] = arrowheadWidest;
			line[lastGoodIndex + 1] = arrowheadBase;
			Keyframe[] keyframes;
			float arrowHeadBaseStart = 1 - arrowSize / dist;
			float arrowHeadBaseWidest = 1 - (arrowSize / dist - distanceBetweenArrowBaseAndWidePoint);
			if (lineKeyFrames == null) {
				keyframes = new Keyframe[] {
					new Keyframe(0, startSize), new Keyframe(arrowHeadBaseStart, endSize),
					new Keyframe(arrowHeadBaseWidest, arrowSize), new Keyframe(1, 0)
				};
			} else {
				// count how many there are after arrowHeadBaseStart.
				int validCount = lineKeyFrames.Length;
				for (int i = 0; i < lineKeyFrames.Length; ++i) {
					float t = lineKeyFrames[i].time;
					if (t > arrowHeadBaseStart) { validCount = i; break; }
				}
				// those are irrelevant now. they'll be replaced by the 3 extra points
				keyframes = new Keyframe[validCount + 3];
				for (int i = 0; i < validCount; ++i) { keyframes[i] = lineKeyFrames[i]; }
				keyframes[validCount + 0] = new Keyframe(arrowHeadBaseStart, endSize);
				keyframes[validCount + 1] = new Keyframe(arrowHeadBaseWidest, arrowSize);
				keyframes[validCount + 2] = new Keyframe(1, 0);
			}
			return keyframes;
		}

		public static LineRenderer MakeArrowBothEnds(ref GameObject lineObject, Vector3 start, Vector3 end,
			Color color = default, float startSize = LINE_SIZE, float endSize = SAME_AS_START_SIZE, float arrowHeadSize = ARROW_SIZE) {
			return MakeArrowBothEnds(ref lineObject, new Vector3[] { end, start }, 2, color, startSize, endSize, arrowHeadSize);
		}
		public static LineRenderer MakeArrowBothEnds(ref GameObject lineObject, IList<Vector3> points, int pointCount,
			Color color = default, float startSize = LINE_SIZE, float endSize = SAME_AS_START_SIZE, float arrowHeadSize = ARROW_SIZE) {
			LineRenderer lr = MakeArrow(ref lineObject, points, pointCount, color, startSize, endSize, arrowHeadSize, null);
			ReverseLineInternal(ref lr);
			Vector3[] p = new Vector3[lr.positionCount];
			lr.GetPositions(p);
			lr = MakeArrow(ref lineObject, p, p.Length, color, endSize, startSize, arrowHeadSize, lr.widthCurve.keys);
			ReverseLineInternal(ref lr);
			return lr;
		}
		public static LineRenderer ReverseLineInternal(ref LineRenderer lr) {
			Vector3[] p = new Vector3[lr.positionCount];
			lr.GetPositions(p);
			Array.Reverse(p);
			lr.SetPositions(p);
			AnimationCurve widthCurve = lr.widthCurve;
			if (widthCurve != null && widthCurve.length > 1) {
				Keyframe[] kf = new Keyframe[widthCurve.keys.Length];
				Keyframe[] okf = widthCurve.keys;
				Array.Copy(okf, kf, okf.Length); //for(int i = 0; i<kf.Length; ++i) { kf[i]=okf[i]; }
				Array.Reverse(kf);
				for (int i = 0; i < kf.Length; ++i) { kf[i].time = 1 - kf[i].time; }
				lr.widthCurve = new AnimationCurve(kf);
			}
			return lr;
		}

		public static LineRenderer MakeArcArrow(ref GameObject lineObj,
			float angle, int pointCount, Vector3 arcPlaneNormal = default, Vector3 firstPoint = default,
			Vector3 center = default, Color color = default, float startSize = LINE_SIZE, float endSize = SAME_AS_START_SIZE, float arrowHeadSize = ARROW_SIZE) {
			if (arcPlaneNormal == default) { arcPlaneNormal = Vector3.up; }
			if (center == default && firstPoint == default) { firstPoint = Vector3.right; }
			Vector3[] points = null;
			LinesMath.WriteArc(ref points, pointCount, arcPlaneNormal, firstPoint, angle, center);
			return MakeArrow(ref lineObj, points, pointCount, color, startSize, endSize, arrowHeadSize);
		}

		public static LineRenderer MakeArcArrowBothEnds(ref GameObject lineObj,
			float angle, int pointCount, Vector3 arcPlaneNormal = default, Vector3 firstPoint = default,
			Vector3 center = default, Color color = default, float startSize = LINE_SIZE, float endSize = SAME_AS_START_SIZE, float arrowHeadSize = ARROW_SIZE) {
			LineRenderer lr = MakeArcArrow(ref lineObj, angle, pointCount, arcPlaneNormal, firstPoint, center, color, startSize, endSize, arrowHeadSize);
			ReverseLineInternal(ref lr);
			Vector3[] p = new Vector3[lr.positionCount];
			lr.GetPositions(p);
			lr = MakeArrow(ref lineObj, p, p.Length, color, endSize, startSize, arrowHeadSize, lr.widthCurve.keys);
			ReverseLineInternal(ref lr);
			return lr;
		}

		public static LineRenderer MakeArcArrow(ref GameObject lineObject, Vector3 start, Vector3 end, Color color = default, float angle = 90, Vector3 upNormal = default,
			float startSize = LINE_SIZE, float endSize = SAME_AS_START_SIZE, float arrowHeadSize = ARROW_SIZE, int pointCount = 0) {
			Vector3[] arc;
			if (end == start || Mathf.Abs(angle) >= 360) {
				arc = new Vector3[] { start, end };
			} else {
				if (upNormal == default) { upNormal = Vector3.up; }
				if (pointCount == 0) { pointCount = Mathf.Max((int)(angle * 24 / 180) + 1, 2); }
				arc = new Vector3[pointCount];
				Vector3 delta = end - start;
				float dist = delta.magnitude;
				Vector3 dir = delta / dist;
				Vector3 right = Vector3.Cross(upNormal, dir).normalized;
				LinesMath.WriteArc(ref arc, pointCount, right, -upNormal, angle);
				Vector3 arcDelta = arc[arc.Length - 1] - arc[0];
				float arcDist = arcDelta.magnitude;
				float angleDiff = Vector3.Angle(arcDelta / arcDist, delta / dist);
				Quaternion turn = Quaternion.AngleAxis(angleDiff, right);
				float ratio = dist / arcDist;
				for (int i = 0; i < arc.Length; ++i) { arc[i] = (turn * arc[i]) * ratio; }
				Vector3 offset = start - arc[0];
				for (int i = 0; i < arc.Length; ++i) { arc[i] += offset; }
			}
			return MakeArrow(ref lineObject, arc, arc.Length, color, startSize, endSize, arrowHeadSize);
		}

		public static void MakeQuaternion(ref GameObject axisObj, Wire[] childWire, Vector3 axis, float angle,
			Vector3 position = default, Color color = default, Quaternion orientation = default,
			int arcPoints = -1, float lineSize = LINE_SIZE, float arrowHeadSize = ARROW_SIZE, Vector3[] startPoint = null) {
			if (childWire.Length != startPoint.Length) { throw new Exception("childWire and startPoint should be parallel arrays"); }
			while (angle >= 180) { angle -= 360; }
			while (angle < -180) { angle += 360; }
			Vector3 axisRotated = orientation * axis;
			MakeArrow(ref axisObj, position - axisRotated, position + axisRotated, color, lineSize, lineSize, arrowHeadSize);
			for (int i = 0; i < childWire.Length; ++i) {
				Wire aObj = childWire[i];
				aObj.Arc(angle, axisRotated, startPoint[i], position, color, Lines.End.Arrow, arcPoints, lineSize);
				//MakeArcArrow(ref aObj, angle, arcPoints, axisRotated, startPoint[i], position, color, lineSize, lineSize, arrowHeadSize);
				childWire[i] = aObj;
			}
		}

		internal static int _CartesianPlaneChildCount(float extents, float increment, out int linesPerDomainHalf) {
			linesPerDomainHalf = (int)(extents / increment);
			if (Mathf.Abs(linesPerDomainHalf - (extents / increment)) < LinesMath.TOLERANCE) --linesPerDomainHalf;
			return 2 + linesPerDomainHalf * 4;
		}
		public static void MakeCartesianPlane(Vector3 center, Vector3 up, Vector3 right, Wire[] wires, Color color = default, float lineWidth = LINE_SIZE,
			float size = .5f, float increments = 0.125f, Vector3 offset = default) {
			// prep data structures
			int wireCount = _CartesianPlaneChildCount(size, increments, out int thinLines);
			while (wires.Length < wireCount) { throw new Exception($"can't make {wireCount} wires with {wires.Length} slots"); }
			Vector3[] endPoints = new Vector3[2];
			// prep math
			Vector3 minX = right * -size, maxX = right * size;
			Vector3 minY = up * -size, maxY = up * size;
			Vector3 p = center + offset;
			int index = 1;
			float thinLineWidth = lineWidth / 4;
			// draw the X and Y axis
			endPoints[0] = p + minX; endPoints[1] = p + maxX; wires[0].Line(endPoints, color, End.Arrow, lineWidth);
			endPoints[0] = p + minY; endPoints[1] = p + maxY; wires[1].Line(endPoints, color, End.Arrow, lineWidth);
			// positiveY
			for (int i = 0; i < thinLines; ++i) {
				Vector3 delta = up * (increments * (i + 1));
				endPoints[0] = p + minX + delta; endPoints[1] = p + maxX + delta;
				wires[++index].Line(endPoints, color, End.Normal, thinLineWidth);
			}
			// negativeY
			for (int i = 0; i < thinLines; ++i) {
				Vector3 delta = -up * (increments * (i + 1));
				endPoints[0] = p + minX + delta; endPoints[1] = p + maxX + delta;
				wires[++index].Line(endPoints, color, End.Normal, thinLineWidth);
			}
			// positiveX
			for (int i = 0; i < thinLines; ++i) {
				Vector3 delta = right * (increments * (i + 1));
				endPoints[0] = p + minY + delta; endPoints[1] = p + maxY + delta;
				wires[++index].Line(endPoints, color, End.Normal, thinLineWidth);
			}
			// negativeX
			for (int i = 0; i < thinLines; ++i) {
				Vector3 delta = -right * (increments * (i + 1));
				endPoints[0] = p + minY + delta; endPoints[1] = p + maxY + delta;
				wires[++index].Line(endPoints, color, End.Normal, thinLineWidth);
			}
		}

		public static void WriteRectangle(Vector3[] out_corner, Vector3 origin, Quaternion rotation, Vector3 halfSize, Vector2 position2D) {
			out_corner[0] = (position2D + new Vector2(-halfSize.x, halfSize.y));
			out_corner[1] = (position2D + new Vector2(halfSize.x, halfSize.y));
			out_corner[2] = (position2D + new Vector2(halfSize.x, -halfSize.y));
			out_corner[3] = (position2D + new Vector2(-halfSize.x, -halfSize.y));
			for (int i = 0; i < 4; ++i) { out_corner[i] = rotation * out_corner[i] + origin; }
		}
		public static void MakeRectangle(Wire[] wires, Vector3 origin, Vector2 position2D, Vector2 halfSize, float lineSize, Color a_color, Quaternion rotation) {
			Vector3[] corners = new Vector3[4];
			WriteRectangle(corners, origin, rotation, halfSize, position2D);
			for (int i = 0; i < corners.Length; ++i) {
				Vector3 a = corners[i];
				Vector3 b = corners[(i + 1) % corners.Length];
				wires[i].Line(a, b, a_color, lineSize).NumCapVertices = 4;
			}
		}

	}
}
