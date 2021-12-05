using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using System.Diagnostics;
#endif

namespace NonStandard {
	/// <summary>cached calculations. used to validate if a line needs to be re-calculated</summary>
	public class Wire : MonoBehaviour {
		public enum Kind { None, Line, Arc, Orbital, SpiralSphere, Box, Quaternion, CartesianPlane, Rectangle, Rod, Disabled }
		private Kind _kind;
		private Vector3[] _points;
		private Vector3 _normal;
		private Quaternion _rotation;
		private int _count;
		private float _startSize, _endSize, _angle;
		private Lines.End _lineEnds;
		public LineRenderer lr;
#if UNITY_EDITOR
		/// <summary>
		/// Where the code is that created this <see cref="Wire"/>. Not present in deployed builds.
		/// </summary>
		// ReSharper disable once NotAccessedField.Global
		public string sourceCode;
#endif
		public Vector3 StartPoint {
			get {
				switch (_kind) {
					case Kind.Rod: return transform.position + _points[0];
					default: return _points[0];
				}
			}
		}
		public Vector3 EndPoint {
			get {
				switch (_kind) {
					case Kind.Rod: return transform.position + _points[_points.Length - 1];
					default: return _points[_points.Length - 1];
				}
			}
		}

		public int NumCapVertices {
			get => lr.numCapVertices;
			set => lr.numCapVertices = value;
		}

		public void RefreshSource() {
#if UNITY_EDITOR
			StackTrace stackTrace = new StackTrace(true);
			StackFrame f = stackTrace.GetFrame(2);
			string path = f.GetFileName();
			if (path == null) return;
			int fileIndex = path.LastIndexOf(System.IO.Path.DirectorySeparatorChar);
			sourceCode = $"{path.Substring(fileIndex + 1)}:{f.GetFileLineNumber().ToString()}";
#endif
		}

		public Kind kind {
			get => _kind;
			set {
				// special cleanup for Quaternions
				if (_kind == Kind.Quaternion && value != Kind.Quaternion) {
					DisposeOfChildWires();
				}
				// special cleanup for CartesianPlanes
				if (_kind == Kind.CartesianPlane && value != Kind.CartesianPlane) {
					DisposeOfChildWires();
				}
				// special cleanup for Rectangles
				if (_kind == Kind.Rectangle && value != Kind.Rectangle) {
					DisposeOfChildWires();
				}
				_kind = value;
			}
		}

		private void DisposeOfChildWires() {
			Wire[] obj = ChildWires(_count, false);
			if (obj != null) {
				Array.ForEach(obj, w => { w.transform.SetParent(null); Destroy(w.gameObject); });
			}
		}

		private static bool SameArrayOfVectors(IList<Vector3> a, IList<Vector3> b) {
			if (ReferenceEquals(a, b)) { return true; }
			if (a == null || b == null || a.Count != b.Count) { return false; }
			for (int i = 0; i < a.Count; ++i) { if (a[i] != b[i]) return false; }
			return true;
		}
		private static bool SameArrayOfVectors(IList<Vector3> a, IList<Vector3> b, Quaternion rotateA, Vector3 offsetA = default) {
			if (ReferenceEquals(a, b)) { return true; }
			if (a == null || b == null || a.Count != b.Count) { return false; }
			for (int i = 0; i < a.Count; ++i) { if (rotateA * a[i] + offsetA != b[i]) return false; }
			return true;
		}
		private bool IsRod(IList<Vector3> points, float startSize, float endSize, Lines.End lineEnds) {
			return kind == Kind.Rod && SameArrayOfVectors(_points, points, transform.rotation, transform.position)
				&& LinesMath.EQ(startSize - _startSize) && LinesMath.EQ(endSize - _endSize) && _lineEnds == lineEnds;
		}
		private bool IsLine(IList<Vector3> points, float startSize, float endSize, Lines.End lineEnds) {
			return kind == Kind.Line && SameArrayOfVectors(_points, points)
				&& LinesMath.EQ(startSize - _startSize) && LinesMath.EQ(endSize - _endSize) && _lineEnds == lineEnds;
		}
		private void SetLine(IList<Vector3> points, float startSize, float endSize, Lines.End lineEnds) {
			kind = Kind.Line;
			if (points != null) {
				_points = new Vector3[points.Count];
				for (int i = 0; i < _points.Length; ++i) { _points[i] = points[i]; }
			}
			//_points = null; // commented this out. was it here for a reason?
			_startSize = startSize; _endSize = endSize; _lineEnds = lineEnds;
		}
		private void SetRod(IList<Vector3> points, float startSize, float endSize, Lines.End lineEnds) {
			kind = Kind.Rod;
			if (points != null) {
				_points = new Vector3[points.Count];
				Vector3 start = points[0];
				Vector3 end = points[points.Count - 1];
				Vector3 delta = end - start;
				//Lines.Make("rod-delta").Arrow(start, end, Color.black, 1f / 128);
				float dist = delta.magnitude;
				Vector3 dir = dist != 0 ? delta / dist : Vector3.forward;
				Vector3 axisOfRotation = Vector3.Cross(Vector3.forward, dir);
				//Lines.Make("rod-axis").Line(start, start+axisOfRotation, Color.magenta, 1f / 128);
				float angleOfRotation;
				if (axisOfRotation != Vector3.zero) {
					axisOfRotation = axisOfRotation.normalized;
					angleOfRotation = Vector3.SignedAngle(Vector3.forward, dir, axisOfRotation);
				} else { axisOfRotation = Vector3.up; angleOfRotation = 0; }
				//Lines.Make("rod-angle").Arc(angleOfRotation, axisOfRotation, Vector3.forward/4, start, Color.gray, startSize:1f / 128);
				Quaternion q = UnityEngine.Quaternion.AngleAxis(angleOfRotation, axisOfRotation);
				Quaternion unq = UnityEngine.Quaternion.AngleAxis(-angleOfRotation, axisOfRotation);
				for (int i = 1; i < _points.Length; ++i) { _points[i] = unq * (points[i] - start); }
				transform.rotation = q;
				transform.position = start;
			}
			//_points = null; // commented this out. was it here for a reason?
			_startSize = startSize; _endSize = endSize; _lineEnds = lineEnds;
		}
		private bool IsArc(Vector3 start, Vector3 normal, Vector3 center, float angle, float startSize, float endSize, Lines.End lineEnds, int pointCount) {
			return kind == Kind.Arc && _points != null && _points.Length == 1 && _points[0] == start && _count == pointCount
				&& _normal == normal && LinesMath.EQ(startSize - _startSize) && LinesMath.EQ(endSize - _endSize) && _lineEnds == lineEnds
				&& transform.position == center && _normal == normal && LinesMath.EQ(_angle - angle);
		}
		private void SetArc(Vector3 start, Vector3 normal, Vector3 center, float angle, float startSize, float endSize, Lines.End lineEnds, int pointCount) {
			kind = Kind.Arc;
			_points = new Vector3[] { start }; _count = pointCount;
			_startSize = startSize; _endSize = endSize; _lineEnds = lineEnds;
			transform.position = center; _normal = normal; _angle = angle;
		}
		private bool IsOrbital(Vector3 start, Vector3 end, Vector3 center, float startSize, float endSize, Lines.End lineEnds, int pointCount) {
			return kind == Kind.Orbital && _points != null && _points.Length == 2 && _count == pointCount
				&& _points[0] == start && _points[1] == end
				&& LinesMath.EQ(startSize - _startSize) && LinesMath.EQ(endSize - _endSize) && _lineEnds == lineEnds
				&& transform.position == center;
		}
		private void SetOrbital(Vector3 start, Vector3 end, Vector3 center = default, float startSize = Lines.LINE_SIZE, float endSize = Lines.SAME_AS_START_SIZE,
			Lines.End lineEnds = default, int pointCount = -1) {
			kind = Kind.Orbital;
			_points = new Vector3[] { start, end }; _count = pointCount;
			_startSize = startSize; _endSize = endSize; _lineEnds = lineEnds;
			transform.position = center;
		}
		private bool IsSpiralSphere(Vector3 center, float radius, float lineSize, Quaternion rotation) {
			return kind == Kind.SpiralSphere
				&& LinesMath.EQ(_startSize - lineSize) && LinesMath.EQ(_endSize - lineSize)
				&& transform.position == center && LinesMath.EQ(_angle - radius)
				&& (_rotation.Equals(rotation) || _rotation == rotation);
		}
		private void SetSpiralSphere(Vector3 center, float radius, float lineSize, Quaternion rotation) {
			kind = Kind.SpiralSphere;
			_startSize = _endSize = lineSize;
			transform.position = center; _angle = radius; _rotation = rotation;
		}
		private bool IsBox(Vector3 center, Vector3 size, Quaternion rotation, float lineSize) {
			Transform t = transform;
			return kind == Kind.Box
				&& LinesMath.EQ(_startSize - lineSize) && LinesMath.EQ(_endSize - lineSize)
				&& t.position == center
				&& t.localScale == size && t.rotation == rotation;
		}
		private void SetBox(Vector3 center, Vector3 size, Quaternion rotation, float lineSize) {
			Transform t = transform;
			kind = Kind.Box;
			_startSize = _endSize = lineSize;
			t.position = center;
			t.localScale = size;
			t.rotation = rotation;
		}
		private bool IsQuaternion(float an, Vector3 ax, Vector3 position, Vector3[] startPoints, Quaternion orientation, float lineSize) {
			return kind == Kind.Quaternion && SameArrayOfVectors(_points, startPoints)
				&& LinesMath.EQ(_startSize - lineSize) && LinesMath.EQ(_endSize - lineSize)
				&& transform.position == position && _normal == ax && LinesMath.EQ(_angle - an) && _count == startPoints.Length
				&& (_rotation.Equals(orientation) || _rotation == orientation); // quaternions can't easily be tested for equality because of floating point errors
		}
		private void SetQuaternion(float an, Vector3 ax, Vector3 position, Vector3[] startPoints, Quaternion orientation, float lineSize) {
			kind = Kind.Quaternion;
			if (ReferenceEquals(startPoints, DefaultQuaternionVisualizationPoints)) {
				_points = DefaultQuaternionVisualizationPoints;
			} else {
				_points = new Vector3[startPoints.Length]; Array.Copy(startPoints, _points, startPoints.Length);
			}
			_startSize = _endSize = lineSize;
			transform.position = position; _normal = ax; _angle = an; _count = startPoints.Length;
			_rotation = orientation;
		}
		private bool IsCartesianPlane(Vector3 center, Quaternion rotation, float lineSize, float extents, float increment) {
			return kind == Kind.CartesianPlane && LinesMath.EQ(_startSize - extents) && LinesMath.EQ(_endSize - lineSize) && LinesMath.EQ(_angle - increment) && (_rotation.Equals(rotation) || _rotation == rotation) && transform.position == center;
		}
		private void SetCartesianPlane(Vector3 center, Quaternion rotation, float lineSize, float extents, float increment) {
			kind = Kind.CartesianPlane; _startSize = extents; _endSize = lineSize; _angle = increment;
			_rotation = rotation; transform.position = center;
			_count = Lines._CartesianPlaneChildCount(extents, increment, out _);
		}
		private bool IsRectangle(Vector3 origin, Vector2 offset2d, Vector2 halfSize, float lineSize, Quaternion rotation) {
			return kind == Kind.Rectangle && origin == transform.position && LinesMath.EQ(_startSize - lineSize) && (_rotation.Equals(rotation) || _rotation == rotation) && LinesMath.EQ(_normal.x - offset2d.x) && LinesMath.EQ(_normal.y - offset2d.y) && LinesMath.EQ(_normal.z - halfSize.x) && LinesMath.EQ(_endSize - halfSize.y);
		}
		private void SetRectangle(Vector3 origin, Vector2 offset2d, Vector2 halfSize, float lineSize, Quaternion rotation) {
			kind = Kind.Rectangle; transform.position = origin; _startSize = lineSize; _rotation = rotation;
			_normal.x = offset2d.x; _normal.y = offset2d.y; _normal.z = halfSize.x; _endSize = halfSize.y; _count = 4;
		}

		public Wire Line(Vector3 start, Vector3 end, Color color = default, float startSize = Lines.LINE_SIZE, float endSize = Lines.SAME_AS_START_SIZE) {
			return Line(new Vector3[] { start, end }, color, Lines.End.Normal, startSize, endSize);
		}
		public Wire Rod(Vector3 start, Vector3 end, Color color = default, float startSize = Lines.LINE_SIZE, float endSize = Lines.SAME_AS_START_SIZE) {
			return Rod(new Vector3[] { start, end }, color, Lines.End.Normal, startSize, endSize);
		}
		public Wire Arrow(Vector3 start, Vector3 end, Color color = default, float startSize = Lines.LINE_SIZE, float endSize = Lines.SAME_AS_START_SIZE) {
			return Line(new Vector3[] { start, end }, color, Lines.End.Arrow, startSize, endSize);
		}
		public Wire Arrow(Vector3 vector, Color color = default, float startSize = Lines.LINE_SIZE, float endSize = Lines.SAME_AS_START_SIZE) {
			return Line(new Vector3[] { Vector3.zero, vector }, color, Lines.End.Arrow, startSize, endSize);
		}
		public Wire Arrow(Ray ray, Color color = default, float startSize = Lines.LINE_SIZE, float endSize = Lines.SAME_AS_START_SIZE) {
			return Line(new Vector3[] { ray.origin, ray.origin + ray.direction }, color, Lines.End.Arrow, startSize, endSize);
		}
		public Wire Bezier(Vector3 start, Vector3 startControl, Vector3 endControl, Vector3 end, Color color = default, Lines.End cap = Lines.End.Normal, float startSize = Lines.LINE_SIZE, int bezierPointCount = 25, float endSize = Lines.SAME_AS_START_SIZE) {
			Vector3[] bezier = new Vector3[bezierPointCount];
			LinesMath.WriteBezier(bezier, start, startControl, endControl, end);
			return Line(bezier, color, cap, startSize, endSize);
		}
		public Wire Line(Vector3 vector, Color color = default, float startSize = Lines.LINE_SIZE, float endSize = Lines.SAME_AS_START_SIZE) {
			return Line(new Vector3[] { Vector3.zero, vector }, color, Lines.End.Normal, startSize, endSize);
		}
		public Wire Line(IList<Vector3> points, Color color = default, Lines.End lineEnds = default, float startSize = Lines.LINE_SIZE, float endSize = Lines.SAME_AS_START_SIZE) {
			if (!IsLine(points, startSize, endSize, lineEnds)) {
				SetLine(points, startSize, endSize, lineEnds);
				if (!lr) { lr = Lines.MakeLineRenderer(gameObject); }
				lr = Lines.MakeLine(lr, points, color, startSize, endSize, lineEnds);
			} //else { Debug.Log("don't need to recalculate line "+name); }
			if (lr) { Lines.SetColor(lr, color); }
			return this;
		}
		public Wire Rod(IList<Vector3> points, Color color = default, Lines.End lineEnds = default, float startSize = Lines.LINE_SIZE, float endSize = Lines.SAME_AS_START_SIZE) {
			if (!IsRod(points, startSize, endSize, lineEnds)) {
				SetRod(points, startSize, endSize, lineEnds);
				if (!lr) { lr = Lines.MakeLineRenderer(gameObject); }
				lr = Lines.MakeLine(lr, _points, color, startSize, endSize, lineEnds);
				lr.useWorldSpace = false;
			} //else { Debug.Log("don't need to recalculate line "+name); }
			if (lr) { Lines.SetColor(lr, color); }
			return this;
		}
		public Wire Arc(float angle, Vector3 normal, Vector3 firstPoint, Vector3 center = default, Color color = default,
			Lines.End lineEnds = default, int pointCount = -1, float startSize = Lines.LINE_SIZE, float endSize = Lines.SAME_AS_START_SIZE) {
			if (pointCount < 0) { pointCount = (int)(24 * angle / 180f) + 1; }
			if (!IsArc(firstPoint, normal, center, angle, startSize, endSize, Lines.End.Normal, pointCount)) {
				SetArc(firstPoint, normal, center, angle, startSize, endSize, Lines.End.Normal, pointCount);
				if (!lr) { lr = Lines.MakeLineRenderer(gameObject); }
				Vector3[] linePoints = null;
				LinesMath.WriteArc(ref linePoints, pointCount, normal, firstPoint, angle, center);
				lr = Lines.MakeLine(lr, linePoints, color, startSize, endSize, lineEnds);
			} //else { Debug.Log("don't need to recalculate arc "+name);  }
			if (LinesMath.EQ2(angle, 360)) { lr.loop = true; }
			Lines.SetColor(lr, color);
			return this;
		}
		public Wire Circle(Vector3 center = default, Vector3 normal = default, Color color = default,
			float radius = 1, float lineSize = Lines.LINE_SIZE, int pointCount = -1) {
			if (LinesMath.EQ2(radius, 0)) { return Line(null, color, Lines.End.Normal, lineSize, lineSize); }
			if (normal == default) { normal = Vector3.up; }
			Vector3 firstPoint = Vector3.zero;
			if (kind == Kind.Arc && this._normal == normal && _points != null && _points.Length > 0) {
				float firstRad = _points[0].magnitude;
				if (LinesMath.EQ2(firstRad, radius)) {
					firstPoint = _points[0];
				} else {
					firstPoint = _points[0] * (radius / firstRad);
				}
			}
			if (firstPoint == Vector3.zero) {
				firstPoint = Vector3.right;
				if (normal != Vector3.up && normal != Vector3.forward && normal != Vector3.back) {
					firstPoint = Vector3.Cross(normal, Vector3.forward).normalized;
				}
				firstPoint *= radius;
			}
			return Arc(360, normal, firstPoint, center, color, Lines.End.Normal, pointCount, lineSize, lineSize);
		}

		/// <summary>
		/// draw line that orbits a sphere with the given center, from the given start to the given end
		/// </summary>
		/// <param name="sphereCenter"></param>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <param name="color"></param>
		/// <param name="lineEnds"></param>
		/// <param name="startSize"></param>
		/// <param name="endSize"></param>
		/// <param name="pointCount"></param>
		/// <returns></returns>
		public Wire Orbital(Vector3 sphereCenter, Vector3 start, Vector3 end,
			Color color = default, Lines.End lineEnds = default, float startSize = Lines.LINE_SIZE, float endSize = Lines.SAME_AS_START_SIZE, int pointCount = -1) {
			if (!IsOrbital(start, end, sphereCenter, startSize, endSize, lineEnds, pointCount)) {
				SetOrbital(start, end, sphereCenter, startSize, endSize, lineEnds, pointCount);
				Vector3[] linePoints = null;
				LinesMath.WriteArcOnSphere(ref linePoints, pointCount, sphereCenter, start, end);
				if (!lr) { lr = Lines.MakeLineRenderer(gameObject); }
				lr = Lines.MakeLine(lr, linePoints, color, startSize, endSize, lineEnds);
			} //else { Debug.Log("don't need to recalculate orbital " + name); }
			Lines.SetColor(lr, color);
			return this;
		}
		public Wire SpiralSphere(Color color = default, Vector3 center = default, float radius = 1, Quaternion rotation = default, float lineSize = Lines.LINE_SIZE) {
			GameObject go = gameObject;
			if (!IsSpiralSphere(center, radius, lineSize, rotation)) {
				SetSpiralSphere(center, radius, lineSize, rotation);
				lr = Lines.MakeSpiralSphere(ref go, radius, center, rotation, color, lineSize);
			} //else { Debug.Log("don't need to recalculate spiral sphere " + name); }
			Lines.SetColor(lr, color);
			return this;
		}
		public Wire Box(Vector3 size, Vector3 center = default, Quaternion rotation = default, Color color = default, float lineSize = Lines.LINE_SIZE) {
			GameObject go = gameObject;
			if (!IsBox(center, size, rotation, lineSize)) {
				SetBox(center, size, rotation, lineSize);
				lr = Lines.MakeBox(ref go, center, size, rotation, color, lineSize);
			} //else { Debug.Log("don't need to recalculate box " + name); }
			Lines.SetColor(lr, color);
			return this;
		}
		private static readonly Vector3[] DefaultQuaternionVisualizationPoints = new Vector3[] { Vector3.forward, Vector3.up };
		public Wire Quaternion(Quaternion q, Color color, Vector3 position = default, Vector3[] startPoints = null,
			Quaternion orientation = default, int arcPoints = -1, float lineSize = Lines.LINE_SIZE) {
			GameObject go = gameObject;
			q.ToAngleAxis(out float an, out Vector3 ax);
			if (startPoints == null) { startPoints = DefaultQuaternionVisualizationPoints; }
			if (!IsQuaternion(an, ax, position, startPoints, orientation, lineSize)) {
				SetQuaternion(an, ax, position, startPoints, orientation, lineSize);
				Wire[] childWires = ChildWires(startPoints.Length, true);
				Lines.MakeQuaternion(ref go, childWires, ax, an, position, color, orientation, arcPoints, lineSize, Lines.ARROW_SIZE, startPoints);
				lr = go.GetComponent<LineRenderer>();
			} //else { Debug.Log("don't need to recalculate quaternion " + name); }
			Lines.SetColor(lr, color);
			return this;
		}
		private Wire[] ChildWires(int objectCount, bool createIfNoneExist) {
			Wire[] wireObjs = null;
			const string _name = "__";
			if (transform.childCount >= objectCount) {
				int childrenWithWire = 0;
				Transform[] children = new Transform[transform.childCount];
				for (int i = 0; i < children.Length; ++i) { children[i] = transform.GetChild(i); }
				Array.ForEach(children, (child) => {
					if (child.name.Contains(_name) && child.GetComponent<Wire>() != null) { ++childrenWithWire; }
				});
				if (childrenWithWire >= objectCount) {
					wireObjs = new Wire[objectCount];
					int validLine = 0;
					for (int i = 0; i < children.Length && validLine < wireObjs.Length; ++i) {
						Wire w;
						if (children[i].name.Contains(_name) && (w = children[i].GetComponent<Wire>()) != null)
							wireObjs[validLine++] = w;
					}
				}
			}
			if (wireObjs == null && createIfNoneExist) {
				wireObjs = new Wire[objectCount];
				for (int i = 0; i < wireObjs.Length; ++i) {
					Wire wireObject = Lines.MakeWire();
					wireObject.name = _name + name + i;
					wireObject.transform.SetParent(transform);
					wireObjs[i] = wireObject;
				}
			}
			return wireObjs;
		}
		public Wire CartesianPlane(Vector3 center, Quaternion rotation, Color color = default, float lineSize = Lines.LINE_SIZE, float extents = 1, float increment = 0.25f) {
			bool colorIsSet = false;
			if (!IsCartesianPlane(center, rotation, lineSize, extents, increment)) {
				SetCartesianPlane(center, rotation, lineSize, extents, increment);
				Vector3 up = rotation * Vector3.up;
				Vector3 right = rotation * Vector3.right;
				Wire[] wires = ChildWires(_count, true);
				Lines.MakeCartesianPlane(center, up, right, wires, color, lineSize, extents, increment);
				colorIsSet = true;
				Vector3 normal = Vector3.Cross(right, up).normalized;
				if (!lr) { lr = Lines.MakeLineRenderer(gameObject); }
				Vector3[] points = new Vector3[] { center, center + normal * increment };
				lr = Lines.MakeLine(lr, points, color, lineSize, lineSize, Lines.End.Arrow);
			} //else { Debug.Log("don't need to recalculate quaternion " + name); }
			if (!colorIsSet) {
				Lines.SetColor(lr, color);
				Wire[] wires = ChildWires(_count, true);
				Array.ForEach(wires, w => Lines.SetColor(w.lr, color));
			}
			return this;
		}
		public Wire Rectangle(Vector3 origin, Vector2 halfSize, Color a_color = default, Quaternion rotation = default, Vector2 offset2d = default, float lineSize = Lines.LINE_SIZE) {
			//if(halfSize == default) { halfSize = Vector2.one / 2; }
			bool colorIsSet = false;
			if (!IsRectangle(origin, offset2d, halfSize, lineSize, rotation)) {
				SetRectangle(origin, offset2d, halfSize, lineSize, rotation);
				Wire[] wires = ChildWires(_count, true);
				Lines.MakeRectangle(wires, origin, offset2d, halfSize, lineSize, a_color, rotation);
				colorIsSet = true;
				if (!lr) { lr = Lines.MakeLineRenderer(gameObject); }
				lr.startWidth = lr.endWidth = 0;
			} //else { Debug.Log("don't need to recalculate quaternion " + name); }
			if (!colorIsSet) {
				//SetColor(lr, a_color);
				Wire[] wires = ChildWires(_count, true);
				Array.ForEach(wires, w => Lines.SetColor(w.lr, a_color));
			}
			return this;
		}
	}
}
