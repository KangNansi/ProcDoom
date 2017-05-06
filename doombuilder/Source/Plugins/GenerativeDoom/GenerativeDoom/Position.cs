using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GenerativeDoom
{
    public enum Direction
    {
        LEFT, RIGHT, UP, DOWN
    }

    class Position : IEqualityComparer<Position>
    {
        public int x, y;

        public Position(int _x, int _y)
        {
            x = _x;
            y = _y;
        }

        public bool Equals(Position a, Position b)
        {
            return (a.x == b.x && a.y == b.y);
        }

        public override bool Equals(Object other)
        {
            return (x == ((Position)other).x && y == ((Position)other).y);
        }

        public override int GetHashCode()
        {
            return x + y;
        }

        public int GetHashCode(Position p)
        {
            return p.x.GetHashCode() + p.y.GetHashCode();
        }

        static public Position operator +(Position p, Position p2)
        {
            return new Position(p.x + p2.x, p.y + p2.y);
        }
    }
}
