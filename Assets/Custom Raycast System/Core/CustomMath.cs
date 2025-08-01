using System;
using System.Collections.Generic;
using System.Linq;

#if UNITY_5_3_OR_NEWER
using UVector3 = UnityEngine.Vector3;
using UQuaternion = UnityEngine.Quaternion;
using UMatrix4x4 = UnityEngine.Matrix4x4;
using UMathf = UnityEngine.Mathf;
using UDebug = UnityEngine.Debug;
#else
    public struct UVector3
    {
        public float x;
        public float y;
        public float z;

        public UVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static UVector3 zero => new UVector3(0, 0, 0);
        public static UVector3 one => new UVector3(1, 1, 1);
        public static UVector3 forward => new UVector3(0, 0, 1);
        public static UVector3 up => new UVector3(0, 1, 0);
        public static UVector3 right => new UVector3(1, 0, 0);

        public float magnitude => (float)Math.Sqrt(x * x + y * y + z * z);
        public UVector3 normalized
        {
            get
            {
                float mag = magnitude;
                if (mag > UMathf.Epsilon) return new UVector3(x / mag, y / mag, z / mag);
                return zero;
            }
        }

        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return x;
                    case 1: return y;
                    case 2: return z;
                    default: throw new IndexOutOfRangeException("Invalid UVector3 index!");
                }
            }
            set
            {
                switch (index)
                {
                    case 0: x = value; break;
                    case 1: y = value; break;
                    case 2: z = value; break;
                    default: throw new IndexOutOfRangeException("Invalid UVector3 index!");
                }
            }
        }

        public static float Dot(UVector3 a, UVector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static UVector3 Cross(UVector3 a, UVector3 b) => new UVector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        public static float Distance(UVector3 a, UVector3 b) => (a - b).magnitude;

        public static UVector3 operator +(UVector3 a, UVector3 b) => new UVector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static UVector3 operator -(UVector3 a, UVector3 b) => new UVector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static UVector3 operator *(UVector3 a, float d) => new UVector3(a.x * d, a.y * d, a.z * d);
        public static UVector3 operator *(float d, UVector3 a) => new UVector3(a.x * d, a.y * d, a.z * d);
        public static UVector3 operator /(UVector3 a, float d) => new UVector3(a.x / d, a.y / d, a.z / d);
        public static UVector3 operator *(UVector3 a, UVector3 b) => new UVector3(a.x * b.x, a.y * b.y, a.z * b.z); // Element-wise multiplication
    }

    public struct UQuaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public UQuaternion(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public static UQuaternion identity => new UQuaternion(0, 0, 0, 1);

        public static UVector3 operator *(UQuaternion rotation, UVector3 point)
        {
            float x = rotation.x * 2F;
            float y = rotation.y * 2F;
            float z = rotation.z * 2F;
            float xx = rotation.x * x;
            float yy = rotation.y * y;
            float zz = rotation.z * z;
            float xy = rotation.x * y;
            float xz = rotation.x * z;
            float yz = rotation.y * z;
            float wx = rotation.w * x;
            float wy = rotation.w * y;
            float wz = rotation.w * z;

            UVector3 res;
            res.x = (1F - (yy + zz)) * point.x + (xy - wz) * point.y + (xz + wy) * point.z;
            res.y = (xy + wz) * point.x + (1F - (xx + zz)) * point.y + (yz - wx) * point.z;
            res.z = (xz - wy) * point.x + (yz + wx) * point.y + (1F - (xx + yy)) * point.z;
            return res;
        }

        public UQuaternion inverse
        {
            get
            {
                float num = x * x + y * y + z * z + w * w;
                if (num == 0f)
                {
                    return identity;
                }
                float num2 = 1f / num;
                return new UQuaternion(-x * num2, -y * num2, -z * num2, w * num2);
            }
        }
    }

    public struct UMatrix4x4
    {
        public float m00, m01, m02, m03;
        public float m10, m11, m12, m13;
        public float m20, m21, m22, m23;
        public float m30, m31, m32, m33;

        public UMatrix4x4(float m00, float m01, float m02, float m03,
                          float m10, float m11, float m12, float m13,
                          float m20, float m21, float m22, float m23,
                          float m30, float m31, float m32, float m33)
        {
            this.m00 = m00; this.m01 = m01; this.m02 = m02; this.m03 = m03;
            this.m10 = m10; this.m11 = m11; this.m12 = m12; this.m13 = m13;
            this.m20 = m20; this.m21 = m21; this.m22 = m22; this.m23 = m23;
            this.m30 = m30; this.m31 = m31; this.m32 = m32; this.m33 = m33;
        }

        public static UMatrix4x4 identity => new UMatrix4x4(
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1
        );

        public static UMatrix4x4 TRS(UVector3 pos, UQuaternion q, UVector3 s)
        {
            UMatrix4x4 m = identity;

            float x = q.x * 2F; float y = q.y * 2F; float z = q.z * 2F;
            float xx = q.x * x; float yy = q.y * y; float zz = q.z * z;
            float xy = q.x * y; float xz = q.x * z; float yz = q.y * z;
            float wx = q.w * x; float wy = q.w * y; float wz = q.w * z;

            m.m00 = (1F - (yy + zz)); m.m01 = (xy - wz); m.m02 = (xz + wy); m.m03 = 0;
            m.m10 = (xy + wz); m.m11 = (1F - (xx + zz)); m.m12 = (yz - wx); m.m13 = 0;
            m.m20 = (xz - wy); m.m21 = (yz + wx); m.m22 = (1F - (xx + yy)); m.m23 = 0;
            m.m30 = 0; m.m31 = 0; m.m32 = 0; m.m33 = 1;

            m.m00 *= s.x; m.m01 *= s.y; m.m02 *= s.z;
            m.m10 *= s.x; m.m11 *= s.y; m.m12 *= s.z;
            m.m20 *= s.x; m.m21 *= s.y; m.m22 *= s.z;

            m.m03 = pos.x;
            m.m13 = pos.y;
            m.m23 = pos.z;

            return m;
        }

        public UMatrix4x4 inverse
        {
            get
            {
                // This inverse assumes a TRS matrix (no shear or perspective).
                // It's optimized for inverse of rotation and translation.
                UMatrix4x4 inv = identity;

                // Extract translation
                UVector3 translation = new UVector3(m03, m13, m23);

                // Inverse rotation (transpose of the 3x3 rotation part)
                inv.m00 = m00; inv.m01 = m10; inv.m02 = m20;
                inv.m10 = m01; inv.m11 = m11; inv.m12 = m21;
                inv.m20 = m02; inv.m21 = m12; inv.m22 = m22;

                // Inverse translation transformed by inverse rotation
                inv.m03 = -(inv.m00 * translation.x + inv.m01 * translation.y + inv.m02 * translation.z);
                inv.m13 = -(inv.m10 * translation.x + inv.m11 * translation.y + inv.m12 * translation.z);
                inv.m23 = -(inv.m20 * translation.x + inv.m21 * translation.y + inv.m22 * translation.z);

                return inv;
            }
        }

        public static UVector3 operator *(UMatrix4x4 m, UVector3 v)
        {
            UVector3 res;
            res.x = m.m00 * v.x + m.m01 * v.y + m.m02 * v.z + m.m03;
            res.y = m.m10 * v.x + m.m11 * v.y + m.m12 * v.z + m.m13;
            res.z = m.m20 * v.x + m.m21 * v.y + m.m22 * v.z + m.m23;
            return res;
        }

        public static UMatrix4x4 operator *(UMatrix4x4 lhs, UMatrix4x4 rhs)
        {
            UMatrix4x4 res = new UMatrix4x4();
            res.m00 = lhs.m00 * rhs.m00 + lhs.m01 * rhs.m10 + lhs.m02 * rhs.m20 + lhs.m03 * rhs.m30;
            res.m01 = lhs.m00 * rhs.m01 + lhs.m01 * rhs.m11 + lhs.m02 * rhs.m21 + lhs.m03 * rhs.m31;
            res.m02 = lhs.m00 * rhs.m02 + lhs.m01 * rhs.m12 + lhs.m02 * rhs.m22 + lhs.m03 * rhs.m32;
            res.m03 = lhs.m00 * rhs.m03 + lhs.m01 * rhs.m13 + lhs.m02 * rhs.m23 + lhs.m03 * rhs.m33;

            res.m10 = lhs.m10 * rhs.m00 + lhs.m11 * rhs.m10 + lhs.m12 * rhs.m20 + lhs.m13 * rhs.m30;
            res.m11 = lhs.m10 * rhs.m01 + lhs.m11 * rhs.m11 + lhs.m12 * rhs.m21 + lhs.m13 * rhs.m31;
            res.m12 = lhs.m10 * rhs.m02 + lhs.m11 * rhs.m12 + lhs.m12 * rhs.m22 + lhs.m13 * rhs.m32;
            res.m13 = lhs.m10 * rhs.m03 + lhs.m11 * rhs.m13 + lhs.m12 * rhs.m23 + lhs.m13 * rhs.m33;

            res.m20 = lhs.m20 * rhs.m00 + lhs.m21 * rhs.m10 + lhs.m22 * rhs.m20 + lhs.m23 * rhs.m30;
            res.m21 = lhs.m20 * rhs.m01 + lhs.m21 * rhs.m11 + lhs.m22 * rhs.m21 + lhs.m23 * rhs.m31;
            res.m22 = lhs.m20 * rhs.m02 + lhs.m21 * rhs.m12 + lhs.m22 * rhs.m22 + lhs.m23 * rhs.m32;
            res.m23 = lhs.m20 * rhs.m03 + lhs.m21 * rhs.m13 + lhs.m22 * rhs.m23 + lhs.m23 * rhs.m33;

            res.m30 = lhs.m30 * rhs.m00 + lhs.m31 * rhs.m10 + lhs.m32 * rhs.m20 + lhs.m33 * rhs.m30;
            res.m31 = lhs.m30 * rhs.m01 + lhs.m31 * rhs.m11 + lhs.m32 * rhs.m21 + lhs.m33 * rhs.m31;
            res.m32 = lhs.m30 * rhs.m02 + lhs.m31 * rhs.m12 + lhs.m32 * rhs.m22 + lhs.m33 * rhs.m32;
            res.m33 = lhs.m30 * rhs.m03 + lhs.m31 * rhs.m13 + lhs.m32 * rhs.m23 + lhs.m33 * rhs.m33;
            return res;
        }
    }

    public static class UMathf
    {
        public const float Epsilon = 1.401298E-45f;
        public const float Infinity = float.PositiveInfinity;
        public static float Abs(float f) => Math.Abs(f);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Sqrt(float f) => (float)Math.Sqrt(f);
        public static float Pow(float f, float p) => (float)Math.Pow(f, p);
        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }

    public static class UDebug
    {
        public static void Log(object message)
        {
            Console.WriteLine(message);
        }
        public static void LogWarning(object message)
        {
            Console.WriteLine("WARNING: " + message);
        }
        public static void LogError(object message)
        {
            Console.Error.WriteLine("ERROR: " + message);
        }
    }
#endif
    