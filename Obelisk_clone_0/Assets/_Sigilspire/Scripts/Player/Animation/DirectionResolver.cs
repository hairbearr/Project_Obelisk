using UnityEngine;

namespace Shared
{
    /// <summary>
    /// Helpers for turning input vectors into 4-way / 8-way directions.
    /// </summary>
    public static class DirectionResolver
    {
        /// <summary>
        /// Returns an 8-way direction index based on the input vector.
        /// 0 = Up, 1 = UpRight, 2 = Right, 3 = DownRight,
        /// 4 = Down, 5 = DownLeft, 6 = Left, 7 = UpLeft.
        /// </summary>
        public static int GetEightWayIndex(Vector2 input)
        {
            if (input.sqrMagnitude < 0.0001f)
                return 0; // default to Up

            float angle = Mathf.Atan2(input.y, input.x); // radians
            float deg = angle * Mathf.Rad2Deg;
            if (deg < 0f) deg += 360f;

            // Each slice = 45 degrees, centered around the main axes
            int index = Mathf.RoundToInt(deg / 45f) % 8;
            return index;
        }

        /// <summary>
        /// Returns a 4-way direction index based on the input vector.
        /// 0 = Up, 1 = Right, 2 = Down, 3 = Left.
        /// </summary>
        public static int GetFourWayIndex(Vector2 input)
        {
            if (input.sqrMagnitude < 0.0001f)
                return 0;

            float angle = Mathf.Atan2(input.y, input.x);
            float deg = angle * Mathf.Rad2Deg;
            if (deg < 0f) deg += 360f;

            if (deg >= 45f && deg < 135f) return 0;   // Up
            if (deg >= 135f && deg < 225f) return 3;  // Left
            if (deg >= 225f && deg < 315f) return 2;  // Down
            return 1;                                 // Right
        }
    }
}
