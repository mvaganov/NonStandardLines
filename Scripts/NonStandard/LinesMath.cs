using System;
using System.Collections.Generic;
using UnityEngine;

namespace NonStandard {
	public static class LinesMath {

		/// <summary>
		/// how close two floating point values need to be before they are considered equal in this library
		/// </summary>
		public const float TOLERANCE = 1f / (1 << 23); // one sixteen-millionth

		public static float Snap(float number, float snap) {
			if (snap == 0) return number;
			snap = Mathf.Abs(snap);
			if (snap <= 1f)
				return Mathf.Floor(number) + (Mathf.Round((number - Mathf.Floor(number)) * (1f / snap)) * snap);
			else
				return Mathf.Round(number / snap) * snap;
		}
		public static float RoundUpToNearest(float n, float snap) {
			if (snap != 0) { return (float)Math.Ceiling(n / snap) * snap; }
			return n;
		}

		public static float RoundDownToNearest(float n, float snap) {
			if (snap != 0) { return (float)Math.Floor(n / snap) * snap; }
			return n;
		}

		public static void IncrementWithSnap(ref float value, float change, ref float snapProgress, float snap, float angleSnapStickiness) {
			if (change == 0) return;
			float lowerBound, upperBound;
			if (value >= 0) {
				lowerBound = LinesMath.RoundDownToNearest(value, snap);
				upperBound = (lowerBound == value) ? value : LinesMath.RoundUpToNearest(value, snap);
			} else {
				upperBound = LinesMath.RoundUpToNearest(value, snap);
				lowerBound = (upperBound == value) ? value : LinesMath.RoundDownToNearest(value, snap);
			}
			IncrementWithSnap(ref value, lowerBound, upperBound, change, ref snapProgress, angleSnapStickiness);
		}
		public static void IncrementWithSnap(ref float value, float lowerBound, float upperBound, float change, ref float snapProgress, float angleSnapStickiness) {
			float excess;
			float newValue = value + change;
			if (change < 0) {
				if (newValue < lowerBound) {
					excess = newValue - lowerBound;
					snapProgress += excess;
					newValue = lowerBound;
				}
				if (snapProgress < -angleSnapStickiness) {
					excess = snapProgress + angleSnapStickiness;
					newValue += excess;
					snapProgress = 0;
				}
			} else {
				if (newValue > upperBound) {
					excess = newValue - upperBound;
					snapProgress += excess;
					newValue = upperBound;
				}
				if (snapProgress > +angleSnapStickiness) {
					excess = snapProgress - angleSnapStickiness;
					newValue += excess;
					snapProgress = 0;
				}
			}
			value = newValue;
		}

		/// <summary>
		/// used to check equality of two floats that are not expected to be assigned as powers of 2
		/// </summary>
		/// <param name="delta">the difference between two floats</param>
		/// <returns></returns>
		public static bool EQ(float delta) { return Mathf.Abs(delta) < LinesMath.TOLERANCE; }

		/// <summary>
		/// intended for use when comparing whole numbers or fractional powers of 2
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool EQ2(float a, float b) {
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			return a == b;
		}

		public static Vector3 GetForwardVector(Quaternion q) {
			return new Vector3(2 * (q.x * q.z + q.w * q.y), 2 * (q.y * q.z + q.w * q.x), 1 - 2 * (q.x * q.x + q.y * q.y));
		}
		public static Vector3 GetUpVector(Quaternion q) {
			return new Vector3(2 * (q.x * q.y + q.w * q.z), 1 - 2 * (q.x * q.x + q.z * q.z), 2 * (q.y * q.z + q.w * q.x));
		}
		public static Vector3 GetRightVector(Quaternion q) {
			return new Vector3(1 - 2 * (q.y * q.y + q.z * q.z), 2 * (q.x * q.y + q.w * q.z), 2 * (q.x * q.z + q.w * q.y));
		}

		/// <summary>Write 2D arc in 3D space, into given Vector3 array</summary>
		/// <param name="points">Will host the list of coordinates</param>
		/// <param name="pointCount">How many vertices to make &gt; 1</param>
		/// <param name="normal">The surface-normal of the arc's plane</param>
		/// <param name="firstPoint">Arc start, rotate about Vector3.zero</param>
		/// <param name="angle">2D angle. Tip: Vector3.Angle(v1, v2)</param>
		/// <param name="offset">How to translate the arc</param>
		/// <param name="startIndex"></param>
		public static void WriteArc(ref Vector3[] points, int pointCount,
			Vector3 normal, Vector3 firstPoint, float angle = 360, Vector3 offset = default, int startIndex = 0) {
			if (pointCount < 0) {
				pointCount = (int)Mathf.Abs(24 * angle / 180f) + 1;
			}
			if (pointCount <= 1) { pointCount = 2; }
			if (pointCount < 0 || pointCount >= 32767) { throw new Exception($"bad point count value: {pointCount}"); }
			if (points == null) { points = new Vector3[pointCount]; }
			if (startIndex >= points.Length) return;
			points[startIndex] = firstPoint;
			Quaternion q = Quaternion.AngleAxis(angle / (pointCount - 1), normal);
			for (int i = startIndex + 1; i < startIndex + pointCount; ++i) { points[i] = q * points[i - 1]; }
			if (offset != Vector3.zero)
				for (int i = startIndex; i < startIndex + pointCount; ++i) { points[i] += offset; }
		}

		public static void WriteBezier(IList<Vector3> points, Vector3 start, Vector3 startControl, Vector3 endControl, Vector3 end, int startIndex = 0, int count = -1) {
			if (count < 0) { count = points.Count - startIndex; }
			float num = count - 1;
			for (int i = 0; i < count; ++i) {
				points[i + startIndex] = GetBezierPoint(start, startControl, endControl, end, i / num);
			}
		}

		public static Vector3 GetBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
			t = Mathf.Clamp01(t); float o = 1 - t, tt = t * t, oo = o * o;
			return oo * o * p0 + 3 * oo * t * p1 + 3 * o * tt * p2 + t * tt * p3;
		}

		public static void WriteArcOnSphere(ref Vector3[] points, int pointCount, Vector3 sphereCenter, Vector3 start, Vector3 end) {
			Vector3 axis;
			if (start == -end) {
				axis = (start != Vector3.up && end != Vector3.up) ? Vector3.up : Vector3.right;
			} else {
				axis = Vector3.Cross(start, end).normalized;
			}
			Vector3 a = start - sphereCenter, b = end - sphereCenter;
			float aRad = a.magnitude, bRad = b.magnitude, angle = 0;
			if (EQ2(aRad, 0) && EQ2(bRad, 0)) {
				a /= aRad; b /= bRad;
				angle = Vector3.Angle(a, b);
				if (float.IsNaN(angle)) { angle = 0; }
			}
			WriteArc(ref points, pointCount, axis, a, angle, Vector3.zero);
			float radDelta = bRad - aRad;
			for (int i = 0; i < points.Length; ++i) {
				points[i] = points[i] * ((i * radDelta / points.Length) + aRad);
				points[i] += sphereCenter;
			}
		}

		public static int WriteCircle(ref Vector3[] points, Vector3 center, Vector3 normal, float radius = 1, int pointCount = 0) {
			if (pointCount == 0) {
				pointCount = (int)Mathf.Round(24 * 3.14159f * radius + 0.5f);
				if (points != null) {
					pointCount = Mathf.Min(points.Length, pointCount);
				}
			}
			Vector3 crossDir = (normal == Vector3.up || normal == Vector3.down) ? Vector3.forward : Vector3.up;
			Vector3 r = Vector3.Cross(normal, crossDir).normalized;
			WriteArc(ref points, pointCount, normal, r * radius, 360, center);
			return pointCount;
		}

		/// <example>CreateSpiralSphere(transform.position, 0.5f, transform.up, transform.forward, 16, 8);</example>
		/// <summary>creates a line spiraled onto a sphere</summary>
		/// <param name="center"></param>
		/// <param name="radius"></param>
		/// <param name="rotation"></param>
		/// <param name="sides"></param>
		/// <param name="rotations"></param>
		/// <returns></returns>
		public static Vector3[] CreateSpiralSphere(Vector3 center = default, float radius = 1,
			Quaternion rotation = default, float sides = 12, float rotations = 6) {
			List<Vector3> points = new List<Vector3>(); // List instead of Array because sides and rotations are floats!
			Vector3 axis = Vector3.up;
			Vector3 axisFace = Vector3.right;
			if (EQ2(sides, 0) && EQ2(rotations, 0)) {
				float iter = 0;
				float increment = 1f / (rotations * sides);
				points.Add(center + axis * radius);
				do {
					iter += increment;
					Quaternion faceTurn = Quaternion.AngleAxis(iter * 360 * rotations, axis);
					Vector3 newFace = faceTurn * axisFace;
					Quaternion q = Quaternion.LookRotation(newFace);
					Vector3 right = GetUpVector(q);
					Vector3 r = right * radius;
					q = Quaternion.AngleAxis(iter * 180, newFace);
					r = q * r;
					r = rotation * r;
					Vector3 newPoint = center + r;
					points.Add(newPoint);
				}
				while (iter < 1);
			}
			return points.ToArray();
		}
	}
}
