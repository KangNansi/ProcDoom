using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GenerativeDoom
{
    
    //Graphe
    class TableGraph<T>
    {
        //Noeud du graphe
        public class TableNode<T>
        {
            public T data;
            public bool right = false;
            public bool left = false;
            public bool up = false;
            public bool down = false;
            public TableNode(T d)
            {
                data = d;
            }
        }

        

        public Dictionary<Position, TableNode<T>> nodes = new Dictionary<Position, TableNode<T>>();

        public void Add(Position key, T value)
        {
            nodes[key] = new TableNode<T>(value);
        }

        public List<Direction> freeNearbyPosition(Position p)
        {
            List<Direction> ret = new List<Direction>();
            if (isEmpty(p + new Position(-1, 0)))
                ret.Add(Direction.LEFT);
            if (isEmpty(p + new Position(1, 0)))
                ret.Add(Direction.RIGHT);
            if (isEmpty(p + new Position(0, 1)))
                ret.Add(Direction.DOWN);
            if (isEmpty(p + new Position(0, -1)))
                ret.Add(Direction.UP);
            return ret;
        }



        public bool isEmpty(Position key)
        {
            return !nodes.ContainsKey(key);
        }

        public Position moveDir(Position key, Direction dir)
        {
            if (dir == Direction.LEFT) return key + new Position(-1, 0);
            if (dir == Direction.RIGHT) return key + new Position(1, 0);
            if (dir == Direction.UP) return key + new Position(0, -1);
            if (dir == Direction.DOWN) return key + new Position(0, 1);
            return key;
        }

        public TableNode<T> this[Position key]
        {
            get
            {
                return nodes[key];
            }
            set
            {
                nodes[key] = value;
            }
        }


    }
}
