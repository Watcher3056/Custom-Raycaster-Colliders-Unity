using System;

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
        public static UVector3 operator -(UVector3 a) => new UVector3(-a.x, -a.y, -a.z);
        public static UVector3 operator *(UVector3 a, float d) => new UVector3(a.x * d, a.y * d, a.z * d);
        public static UVector3 operator *(float d, UVector3 a) => new UVector3(a.x * d, a.y * d, a.z * d);
        public static UVector3 operator /(UVector3 a, float d) => new UVector3(a.x / d, a.y / d, a.z / d);
        // No element-wise operator* on purpose: UnityEngine.Vector3 has none (use Vector3.Scale).
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

        // Unity-parity: UnityEngine.Quaternion has only the static Inverse (no instance .inverse).
        public static UQuaternion Inverse(UQuaternion rotation)
        {
            float num = rotation.x * rotation.x + rotation.y * rotation.y + rotation.z * rotation.z + rotation.w * rotation.w;
            if (num == 0f)
            {
                return identity;
            }
            float num2 = 1f / num;
            return new UQuaternion(-rotation.x * num2, -rotation.y * num2, -rotation.z * num2, rotation.w * num2);
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
                // Full cofactor inverse: correct for any TRS, including non-uniform scale.
                float a00 = m00, a01 = m01, a02 = m02, a03 = m03;
                float a10 = m10, a11 = m11, a12 = m12, a13 = m13;
                float a20 = m20, a21 = m21, a22 = m22, a23 = m23;
                float a30 = m30, a31 = m31, a32 = m32, a33 = m33;

                float b00 = a00 * a11 - a01 * a10;
                float b01 = a00 * a12 - a02 * a10;
                float b02 = a00 * a13 - a03 * a10;
                float b03 = a01 * a12 - a02 * a11;
                float b04 = a01 * a13 - a03 * a11;
                float b05 = a02 * a13 - a03 * a12;
                float b06 = a20 * a31 - a21 * a30;
                float b07 = a20 * a32 - a22 * a30;
                float b08 = a20 * a33 - a23 * a30;
                float b09 = a21 * a32 - a22 * a31;
                float b10 = a21 * a33 - a23 * a31;
                float b11 = a22 * a33 - a23 * a32;

                float det = b00 * b11 - b01 * b10 + b02 * b09 + b03 * b08 - b04 * b07 + b05 * b06;
                if (det == 0f) return identity;
                float invDet = 1f / det;

                UMatrix4x4 inv = new UMatrix4x4();
                inv.m00 = (a11 * b11 - a12 * b10 + a13 * b09) * invDet;
                inv.m01 = (-a01 * b11 + a02 * b10 - a03 * b09) * invDet;
                inv.m02 = (a31 * b05 - a32 * b04 + a33 * b03) * invDet;
                inv.m03 = (-a21 * b05 + a22 * b04 - a23 * b03) * invDet;
                inv.m10 = (-a10 * b11 + a12 * b08 - a13 * b07) * invDet;
                inv.m11 = (a00 * b11 - a02 * b08 + a03 * b07) * invDet;
                inv.m12 = (-a30 * b05 + a32 * b02 - a33 * b01) * invDet;
                inv.m13 = (a20 * b05 - a22 * b02 + a23 * b01) * invDet;
                inv.m20 = (a10 * b10 - a11 * b08 + a13 * b06) * invDet;
                inv.m21 = (-a00 * b10 + a01 * b08 - a03 * b06) * invDet;
                inv.m22 = (a30 * b04 - a31 * b02 + a33 * b00) * invDet;
                inv.m23 = (-a20 * b04 + a21 * b02 - a23 * b00) * invDet;
                inv.m30 = (-a10 * b09 + a11 * b07 - a12 * b06) * invDet;
                inv.m31 = (a00 * b09 - a01 * b07 + a02 * b06) * invDet;
                inv.m32 = (-a30 * b03 + a31 * b01 - a32 * b00) * invDet;
                inv.m33 = (a20 * b03 - a21 * b01 + a22 * b00) * invDet;
                return inv;
            }
        }

        // Headless equivalents of the UnityEngine.Matrix4x4 members the Core relies on.
        public UVector3 MultiplyPoint3x4(UVector3 point)
        {
            UVector3 res;
            res.x = m00 * point.x + m01 * point.y + m02 * point.z + m03;
            res.y = m10 * point.x + m11 * point.y + m12 * point.z + m13;
            res.z = m20 * point.x + m21 * point.y + m22 * point.z + m23;
            return res;
        }

        public UVector3 MultiplyVector(UVector3 vector)
        {
            UVector3 res;
            res.x = m00 * vector.x + m01 * vector.y + m02 * vector.z;
            res.y = m10 * vector.x + m11 * vector.y + m12 * vector.z;
            res.z = m20 * vector.x + m21 * vector.y + m22 * vector.z;
            return res;
        }

        public UMatrix4x4 transpose
        {
            get
            {
                UMatrix4x4 t = identity;
                t.m00 = m00; t.m01 = m10; t.m02 = m20; t.m03 = m30;
                t.m10 = m01; t.m11 = m11; t.m12 = m21; t.m13 = m31;
                t.m20 = m02; t.m21 = m12; t.m22 = m22; t.m23 = m32;
                t.m30 = m03; t.m31 = m13; t.m32 = m23; t.m33 = m33;
                return t;
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
