using System;
using System.Collections.Generic;
using System.Linq;

namespace SquareGrid
{
    /// <summary> Enumeration describing a direction in 2-D space. </summary>
    public enum Direction
    {
        /// <summary> Obligatory 'Undefined' enumeration value. </summary>
        Undefined = 0,

        /// <summary> Enumeration describing the 'Y-Positive' direction. </summary>
        PositiveY,

        /// <summary> Enumeration describing the 'X-Positive' direction. </summary>
        PositiveX,

        /// <summary> Enumeration describing the 'Y-Negative' direction. </summary>
        NegativeY,

        /// <summary> Enumeration describing the 'X-Negative' direction. </summary>
        NegativeX,


        /// <summary> Enumeration describing the diagonal direction where both X and Y are positive. </summary>
        DiagonalPosXPosY,

        /// <summary> Enumeration describing the diagonal direction where X is positive and Y is negative. </summary>
        DiagonalPosXNegY,

        /// <summary> Enumeration describing the diagonal direction where both X and Y are negative. </summary>
        DiagonalNegXNegY,

        /// <summary> Enumeration describing the diagonal direction where X is negative and Y are positive. </summary>
        DiagonalNegXPosY
    }

    /// <summary> A struct which represents a discrete point in 2-D space. </summary>
    public readonly struct Coordinate : IEquatable<Coordinate>
    {
        /// <summary> The X value of this point in 2-D space. </summary>
        public readonly int X;

        /// <summary> The Y value of this point in 2-D space. </summary>
        public readonly int Y;


        /// <summary> Creates a new <see cref="Coordinate"/> struct with the given [x] and [y] values. </summary>
        /// <param name="x"> The value to assign to this <see cref="Coordinate"/> struct's X value. </param>
        /// <param name="y"> The value to assign to this <see cref="Coordinate"/> struct's Y value. </param>
        public Coordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary> Returns a new <see cref="Coordinate"/> struct with values (0, 0). </summary>
        public static Coordinate Zero => new Coordinate(0, 0);

        /// <summary> Returns a new <see cref="Coordinate"/> struct with values (1, 1). </summary>
        public static Coordinate One => new Coordinate(1, 1);

        /// <summary> Returns a <see cref="Tuple"/> representation of this <see cref="Coordinate"/>. </summary>
        public (int X, int Y) AsTuple => (X, Y);

        /// <summary> Method whose purpose is to resolve diagonal <see cref="Direction"/> values into orthogonal values. </summary>
        /// <param name="input"> The given <see cref="Direction"/> to resolve into an orthogonal direction. </param>
        /// <param name="preferResolveToX"> Dictates whether diagonal <see cref="Direction"/> values will resolve to their orthogonal X or Y portions.</param>
        public static Direction SimplifyDirection(Direction input, bool preferResolveToX)
        {
            switch (input)
            {
                case Direction.PositiveY:
                case Direction.PositiveX:
                case Direction.NegativeY:
                case Direction.NegativeX:
                    return input;

                case Direction.DiagonalPosXPosY: return preferResolveToX ? Direction.PositiveX : Direction.PositiveY;
                case Direction.DiagonalPosXNegY: return preferResolveToX ? Direction.PositiveX : Direction.NegativeY;
                case Direction.DiagonalNegXPosY: return preferResolveToX ? Direction.NegativeX : Direction.PositiveY;
                case Direction.DiagonalNegXNegY: return preferResolveToX ? Direction.NegativeX : Direction.NegativeY;

                case Direction.Undefined:
                default: return Direction.Undefined;
            }
        }



        /// <summary> Returns a set of <see cref="Coordinate"/> structs with their distance from this <see cref="Coordinate"/>, which were reachable given [obstacles]. </summary>
        /// <param name="maxDistance"> The maximum distance to flood-fill away this <see cref="Coordinate"/> before returning the result. </param>
        /// <param name="obstacles"> A set of <see cref="Coordinate"/> structs which are not allowed to be flood-filled to. </param>
        public Dictionary<Coordinate, int> FloodFill(int maxDistance, IEnumerable<Coordinate> obstacles = null) => FloodFill(new[] { this }, maxDistance, obstacles);

        /// <summary> Returns a set of <see cref="Coordinate"/> structs with their distance from one of the [input] <see cref="Coordinate"/> structs, which were reachable given [obstacles]. </summary>
        /// <param name="startCoords"> The starting <see cref="Coordinate"/> values for the flood-fill algorithm. </param>
        /// <param name="maxDistance"> The maximum distance to flood-fill away any of the [input] <see cref="Coordinate"/> values before returning the result. </param>
        /// <param name="obstacles"> A set of <see cref="Coordinate"/> structs which are not allowed to be flood-filled to. </param>
        public static Dictionary<Coordinate, int> FloodFill(IEnumerable<Coordinate> startCoords, int maxDistance, IEnumerable<Coordinate> obstacles = null)
        {
            var obstaclesList = obstacles?.ToList() ?? new List<Coordinate>();
            var prev = new List<Coordinate>(startCoords);
            var ret = prev.ToDictionary(
                coord => coord,
                _ => 0
            );

            for (int i = 1; i <= maxDistance; i++)
            {
                var current = new List<Coordinate>();
                foreach (var coord in prev)
                {
                    var adjacentCoords = coord.GetAdjacents();
                    foreach (var candidate in adjacentCoords)
                    {
                        if (ret.Any(x => x.Key == candidate)) continue;
                        if (current.Any(x => x == candidate)) continue;
                        if (obstaclesList.Any(x => x == candidate)) continue;

                        current.Add(candidate);
                        ret.Add(candidate, i);
                    }
                }

                prev = current;
            }

            return ret;
        }

        /// <summary> Returns a set of <see cref="Coordinate"/> values which describe a line starting from this <see cref="Coordinate"/> to the [end] <see cref="Coordinate"/>. </summary>
        /// <param name="endpoint"> The <see cref="Coordinate"/> to end the line at. </param>
        public IEnumerable<Coordinate> GetLineToPoint(Coordinate endpoint)
        {
            int length = GetDistanceTo_Diagonal(endpoint);
            float step = 1.0f / Math.Max(length, 1);

            var ret = new List<Coordinate> { this };
            for (int i = 1; i <= length; i++)
            {
                float t = step * i;
                ret.Add(Lerp(this, endpoint, t));
            }

            return ret;
        }

        /// <summary> Returns a <see cref="Direction"/> which indicates the direction from this <see cref="Coordinate"/> to [other]. </summary>
        /// <param name="other"> The <see cref="Coordinate"/> which a direction to is being calculated from this <see cref="Coordinate"/>. </param>
        public Direction GetDirectionToCoord(Coordinate other)
        {
            if (this == other) return Direction.Undefined;
            var diff = other - this;
            var abs = diff.Abs();

            if (abs.X == abs.Y)
            {
                if (diff.X > 0)
                    return (diff.Y > 0) ? Direction.DiagonalPosXPosY : Direction.DiagonalPosXNegY;
                return (diff.Y > 0) ? Direction.DiagonalNegXPosY : Direction.DiagonalNegXNegY;
            }

            if (abs.X > abs.Y)
                return (abs.X > 0) ? Direction.PositiveX : Direction.NegativeX;
            return (abs.Y > 0) ? Direction.PositiveY : Direction.NegativeY;
        }

        /// <summary> Returns a <see cref="Coordinate"/> vector based on the supplied <see cref="Direction"/> input. </summary>
        /// <param name="dir"> The <see cref="Direction"/> to get a vector for. </param>
        public static Coordinate GetVectorFromDirection(Direction dir)
        {
            switch (dir)
            {
                case Direction.PositiveY: return new Coordinate(0, 1);
                case Direction.PositiveX: return new Coordinate(1, 0);
                case Direction.NegativeY: return new Coordinate(0, -1);
                case Direction.NegativeX: return new Coordinate(-1, 0);

                case Direction.DiagonalPosXPosY: return new Coordinate(1, 1);
                case Direction.DiagonalPosXNegY: return new Coordinate(1, -1);
                case Direction.DiagonalNegXNegY: return new Coordinate(-1, -1);
                case Direction.DiagonalNegXPosY: return new Coordinate(-1, 1);

                case Direction.Undefined:
                default: return Coordinate.Zero;
            }
        }

        /// <summary> Returns a <see cref="Coordinate"/> struct whose X and Y values are each different by 1 or less, in the given <see cref="Direction"/>. </summary>
        /// <param name="dir"> The <see cref="Direction"/> in which to get the <see cref="Coordinate"/> value adjacent to this one. </param>
        public Coordinate GetNeighborInDirection(Direction dir) => (this + GetVectorFromDirection(dir));

        /// <summary> Returns the four <see cref="Coordinate"/> structs whose values are adjacent to this one. </summary>
        public IEnumerable<Coordinate> GetAdjacents()
        {
            var ret = new List<Coordinate>
            {
                GetNeighborInDirection(Direction.PositiveY),
                GetNeighborInDirection(Direction.PositiveX),

                GetNeighborInDirection(Direction.NegativeY),
                GetNeighborInDirection(Direction.NegativeX)
            };

            return ret;
        }


        /// <summary> Returns a <see cref="bool"/> indicating whether the <see cref="Coordinate"/> line to [other] is interrupted by any of the [obstacles]. </summary>
        /// <param name="other"> The end-point <see cref="Coordinate"/> of the line to be created. </param>
        /// <param name="obstacles"> The set of <see cref="Coordinate"/> structs which are considered obstacles to interrupt the line. </param>
        public bool IsLineToCoordinateUnbroken(Coordinate other, IEnumerable<Coordinate> obstacles = null) =>
            !GetLineToPoint(other).Intersect(obstacles ?? new Coordinate[0]).Any();

        /// <summary> Returns a <see cref="bool"/> value indicating whether this <see cref="Coordinate"/> struct and [other] are equal. </summary>
        /// <param name="other"> The <see cref="Coordinate"/> struct to compare this <see cref="Coordinate"/> to. </param>
        public bool Equals(Coordinate other) => (this == other);

        /// <summary> Returns a <see cref="bool"/> value indicating whether this <see cref="Coordinate"/> struct and the <see cref="object"/> [other] are equal  </summary>
        /// <param name="obj"> The <see cref="object"/> which this <see cref="Coordinate"/> is being compared to. </param>
        public override bool Equals(object obj) => (obj is Coordinate coord) && (this == coord);


        /// <summary> Returns the greater absolute value of the two: (this.X - other.X) and (this.Y - other.Y). </summary>
        /// <param name="other"> The <see cref="Coordinate"/> value to get the diagonal distance to. </param>
        public int GetDistanceTo_Diagonal(Coordinate other)
        {
            var abs = (this - other).Abs();
            return Math.Max(abs.X, abs.Y);
        }

        /// <summary> Returns the sum of absolute values of: (this.X - other.X) and (this.Y - other.Y). </summary>
        /// <param name="other"> The other <see cref="Coordinate"/> struct to calculate distance between. </param>
        public int GetDistanceTo_Cardinal(Coordinate other)
        {
            var abs = (this - other).Abs();
            return abs.X + abs.Y;
        }


        /// <summary> Returns a new <see cref="Coordinate"/> struct whose values are the absolute values of this <see cref="Coordinate"/> struct. </summary>
        public Coordinate Abs() => new Coordinate(Math.Abs(X), Math.Abs(Y));

        /// <summary> Returns a new <see cref="Coordinate"/> struct whose X and Y values are a transform of the difference of the two coordinates, through the <see cref="Coordinate"/> [a]. </summary>
        /// <param name="a"> The 'starting' <see cref="Coordinate"/> value for the interpolation. </param>
        /// <param name="b"> The 'ending' <see cref="Coordinate"/> value for the interpolation. </param>
        /// <param name="t"> The value to use as the transformation between the two [a] and [b] <see cref="Coordinate"/> structs. </param>
        public static Coordinate Lerp(Coordinate a, Coordinate b, float t) => a + (t * (b - a));


        /// <summary> Returns a new <see cref="Coordinate"/> struct whose X value is equal to [a].X - [b].X, and whose Y value is equal to [a].Y - [b].Y </summary>
        /// <param name="a"> One of the two <see cref="Coordinate"/> structs to be added together. </param>
        /// <param name="b"> The other of the two <see cref="Coordinate"/> structs to be added together. </param>
        public static Coordinate operator +(Coordinate a, Coordinate b) => new Coordinate(a.X + b.X, a.Y + b.Y);

        /// <summary> Returns a new <see cref="Coordinate"/> objstructect whose X value is equal to [a].X - [b].X, and whose Y value is equal to [a].Y - [b].Y </summary>
        /// <param name="a"> One of the two <see cref="Coordinate"/> structs to be added together. </param>
        /// <param name="b"> The other of the two <see cref="Coordinate"/> structs to be added together. </param>
        public static Coordinate operator -(Coordinate a, Coordinate b) => new Coordinate(a.X - b.X, a.Y - b.Y);


        /// <summary> Returns a new <see cref="Coordinate"/> struct whose values equal the <see cref="Coordinate"/> struct's values multiplied by [t], and then rounded to the nearest integer.
        /// <para> Points to the [*] operator which accepts a <see cref="Coordinate"/> as the first parameter and a <see cref="float"/> value as the second. </para></summary>
        /// <param name="coord"> The <see cref="Coordinate"/> struct to multiply the values of by [t]. </param>
        /// <param name="mult"> The value to multiply the given <see cref="Coordinate"/> struct's values by. </param>
        public static Coordinate operator *(float mult, Coordinate coord) => coord * mult;

        /// <summary> Returns a new <see cref="Coordinate"/> struct whose values equal the <see cref="Coordinate"/> struct's values multiplied by [t], and then rounded to the nearest integer. </summary>
        /// <param name="coord"> The <see cref="Coordinate"/> struct to multiply the values of by [t]. </param>
        /// <param name="mult"> The value to multiply the given <see cref="Coordinate"/> struct's values by. </param>
        public static Coordinate operator *(Coordinate coord, float mult) =>
            new Coordinate((int)Math.Round(mult * coord.X), (int)Math.Round(mult * coord.Y));

        /// <summary> Returns a new <see cref="Coordinate"/> struct whose values equal the <see cref="Coordinate"/> struct's values multiplied by [t].
        /// <para> Points to the [*] operator which accepts a <see cref="Coordinate"/> as the first parameter and a <see cref="int"/> value as the second. </para></summary>
        /// <param name="coord"> The <see cref="Coordinate"/> struct to multiply the values of by [t]. </param>
        /// <param name="mult"> The value to multiply the given <see cref="Coordinate"/> struct's values by. </param>
        public static Coordinate operator *(int mult, Coordinate coord) => coord * mult;

        /// <summary> Returns a new <see cref="Coordinate"/> struct whose values equal the <see cref="Coordinate"/> struct's values multiplied by [t]. </summary>
        /// <param name="coord"> The <see cref="Coordinate"/> struct to multiply the values of by [t]. </param>
        /// <param name="mult"> The value to multiply the given <see cref="Coordinate"/> struct's values by. </param>
        public static Coordinate operator *(Coordinate coord, int mult) => new Coordinate(mult * coord.X, mult * coord.Y);


        /// <summary> Returns a boolean value indicating whether the two given <see cref="Coordinate"/> structs are equal. </summary>
        /// <param name="a"> The first of the two <see cref="Coordinate"/> structs to be compared to each other. </param>
        /// <param name="b"> The second of the two <see cref="Coordinate"/> structs to be compared to each other. </param>
        public static bool operator ==(Coordinate a, Coordinate b) => (a.X == b.X) && (a.Y == b.Y);

        /// <summary> Returns a boolean value indicating whether the two given <see cref="Coordinate"/> structs are NOT equal. </summary>
        /// <param name="a"> The first of the two <see cref="Coordinate"/> structs to be compared to each other. </param>
        /// <param name="b"> The second of the two <see cref="Coordinate"/> structs to be compared to each other. </param>
        public static bool operator !=(Coordinate a, Coordinate b) => (a.X != b.X) || (a.Y != b.Y);

        /// <summary> Implicit conversion from a <see cref="Tuple"/>&lt;<see cref="int"/>, <see cref="int"/>&gt; to a <see cref="Coordinate"/>. </summary>
        /// <param name="tuple"> The <see cref="Tuple"/> to implicitly convert to a <see cref="Coordinate"/>. </param>
        public static implicit operator Coordinate((int X, int Y) tuple) => new Coordinate(tuple.X, tuple.Y);

        /// <summary> Returns a <see cref="string"/> representation of this <see cref="Coordinate"/> struct. </summary>
        public override string ToString() => $"({X}, {Y})";

        /// <summary> Returns a HashCode for this <see cref="Coordinate"/> struct, based on it's X and Y values. </summary>
        public override int GetHashCode()
        {
            var hashCode = 1861411795;
            hashCode = (hashCode * -1521134295) + X.GetHashCode();
            hashCode = (hashCode * -1521134295) + Y.GetHashCode();
            return hashCode;
        }
    }
}