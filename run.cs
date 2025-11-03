using System;
using System.Collections.Generic;
using System.Diagnostics;

class Program
{
    enum TileType
    {
        Empty, Wall, Hall, Room
    }

    struct Dot
    {
        public int X;
        public int Y;

        public Dot(int y, int x)
        {
            this.X = x;
            this.Y = y;
        }

        public static Dot operator +(Dot a, Dot b) => new Dot(a.Y + b.Y, a.X + b.X);
        public static bool operator ==(Dot a, Dot b) => (a.X == b.X && a.Y == b.Y);
        public static bool operator !=(Dot a, Dot b) => !(a == b);
    }


    struct Tile
    {
        public TileType type;
        public bool occupiable;
        public bool isEntrance;
        public char? occupant;
        public char? expectedObject;
        public bool isOccupied => occupant != null;
        public bool objectIsCorrect => isOccupied && occupant == expectedObject;

        public Tile(TileType type, bool occupiable, bool isEntrance = false,  char? occupant = null, char? expectedObject = null)
        {
            this.type = type;
            this.occupiable = occupiable;
            this.isEntrance = isEntrance;
            this.occupant = occupant;
            this.expectedObject = expectedObject;
        }

        public char ToChar()
        {
            if (isOccupied)
                return occupant.Value;
            switch (type)
            {
                case TileType.Wall:
                    return '#';

                case TileType.Hall:
                    return '.';

                case TileType.Room:
                    return occupant is null ? ' ' : occupant.Value;

                case TileType.Empty:
                default:
                    return ' ';
            }
        }
    }

    class Board
    {
        public Tile[][] map;
        private int sizeX;
        private int sizeY;

        public Board(List<string> input)
        {
            sizeY = input.Count;
            sizeX = input[0].Length;
            map = new Tile[sizeY][];

            for (int y = 0; y < sizeY; y++)
            {
                map[y] = new Tile[sizeX];

                for (int x = 0; x < sizeX; x++)
                {
                    char c = x >= input[y].Length ? ' ' : input[y][x];

                    Tile tile;

                    switch (c)
                    {

                        case '#':
                            tile = new Tile(TileType.Wall, false);
                            break;

                        case '.':
                            tile = new Tile(TileType.Hall, true, input[y + 1][x] != '#');
                            break;

                        case 'A':
                        case 'B':
                        case 'C':
                        case 'D':
                            tile = x switch
                            {
                                3 => new Tile(TileType.Room, true, false, c, 'A'),
                                5 => new Tile(TileType.Room, true, false, c, 'B'),
                                7 => new Tile(TileType.Room, true, false, c, 'C'),
                                9 => new Tile(TileType.Room, true, false, c, 'D')
                            };

                            break;

                        case ' ':
                        default:
                            tile = new Tile(TileType.Empty, false);
                            break;
                    }

                    map[y][x] = tile;
                }
            }
        }

        public Board(Tile[][] tiles)
        {
            map = tiles;
            sizeY = tiles.Length;
            sizeX = tiles[0].Length;
        }

        public Board MoveObject(Dot start, Dot finish)
        {
            var newMap = new Tile[sizeY][];

            for (int y = 0; y < sizeY; y++)
            {
                newMap[y] = new Tile[sizeX];

                for (int x = 0; x < sizeX; x++)
                {
                    if (y == start.Y && x == start.X)
                    {
                        var startTile = map[y][x];
                        newMap[y][x] = startTile with
                        {
                            occupant = null
                        };
                    }
                    else if (y == finish.Y && x == finish.X)
                    {
                        var finishTile = map[y][x];
                        newMap[y][x] = finishTile with
                        {
                            occupant = (map[start.Y][start.X]).occupant
                        };
                    }
                    else
                    {
                        newMap[y][x] = map[y][x];
                    }
                }
            }

            return new Board(newMap);
        }

        public IEnumerable<(Board World, int EnergyCost)> GetPossibleMoves()
        {
            var movableObjects = AllTiles
                .Where(x => x.tile.occupiable && x.tile.isOccupied)
                .Select(x => (x.tile, x.pos));

            var result = new List<(Board World, int EnergyCost)>();

            foreach (var movableObject in movableObjects)
            {
                var objectType = movableObject.tile.occupant;

                if (movableObject.tile.type == TileType.Room && movableObject.tile.objectIsCorrect)
                {
                    bool blockingBelow = Enumerable.Range(movableObject.pos.Y, sizeY - movableObject.pos.Y - 1)
                        .Select(y => Get(new Dot(y, movableObject.pos.X)))
                        .Any(f => f.type == TileType.Room && !f.objectIsCorrect);

                    if (!blockingBelow) continue;
                }

                foreach (var destination in GetReachableTiles(movableObject.pos))
                {
                    var targetTile = Get(destination.pos);

                    if(targetTile.isEntrance) continue;

                    if (movableObject.tile.type == TileType.Hall)
                    {
                        if (targetTile.type == TileType.Hall) continue;

                        if (targetTile.type == TileType.Room && targetTile.expectedObject != objectType) continue;
                    }

                    if (targetTile.type == TileType.Room)
                    {
                        if (targetTile.expectedObject != objectType) continue;

                        if (movableObject.tile.type == TileType.Room && targetTile.expectedObject == movableObject.tile.expectedObject) continue;

                        Tile tileBelow = map[destination.pos.Y + 1][destination.pos.X];
                        if (tileBelow.type == TileType.Room && !tileBelow.isOccupied) continue;

                        if (AllTiles.Where(t => t.tile.type == TileType.Room).Select(a => a.pos).Select(r => Get(r)).Any(r => r.expectedObject == targetTile.expectedObject && r.isOccupied && !r.objectIsCorrect))
                            continue;
                    }

                    var cost = destination.Distance * objectType switch
                    {
                        'A' => 1,
                        'B' => 10,
                        'C' => 100,
                        'D' => 1000
                    };

                    result.Add((MoveObject(movableObject.pos, destination.pos), cost));
                }
            }

            return result;
        }

        private IEnumerable<(Tile tile, Dot pos)> AllTiles
        => map.SelectMany((row, y) => row.Select((tile, x) => (tile, new Dot(y, x))));

        public Tile Get(int Y, int X) => map[Y][X];
        public Tile Get(Dot dot) => map[dot.Y][dot.X];

        private List<(Dot pos, int Distance)> GetReachableTiles(Dot from)
        {
            var visited = new List<(Dot, int)>();

            void GetReachableFieldsHelper(Dot start, int distance)
            {
                var tilesAround = Neighbourhs(start)
                    .Where(n => Get(n) is Tile field && field.occupiable && !field.isOccupied && !visited.Any(m => m.Item1 == n));

                foreach (var field in tilesAround)
                {
                    visited.Add((field, distance));
                    GetReachableFieldsHelper(field, distance + 1);
                }
            }

            GetReachableFieldsHelper(from, 1);

            return visited;
        }

        private IEnumerable<Dot> Neighbourhs(Dot coor) => new[]
        {
        coor + new Dot(0, 1),
        coor + new Dot(1, 0),
        coor + new Dot(-1, 0),
        coor + new Dot(0, -1),
    }.Where(n => n.Y < sizeY && n.X < sizeX && n.Y >= 0 && n.X >= 0);

        public override string ToString() => new(
        Enumerable.Range(0, sizeY).SelectMany(y =>
        Enumerable.Range(0, sizeX).Select(x => map[y][x].ToChar()).Append('\r').Append('\n'))
        .ToArray());

        public override int GetHashCode() => ToString().GetHashCode();
    }

    static int Solve(List<string> lines)
    {
        Board startState = new Board(lines);
        Board solvedState = GetFinishedBoard(lines.Count);
        if (solvedState == null) return 0;
        var queue = new PriorityQueue<Board, int>();
        queue.Enqueue(startState, 0);

        var lowestEnergies = new Dictionary<int, int>
        {
            [startState.GetHashCode()] = 0
        };

        var previousWorld = new Dictionary<int, (Board, int)>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var currentEnergy = lowestEnergies[current.GetHashCode()];

            foreach (var move in current.GetPossibleMoves())
            {
                var hash = move.World.GetHashCode();
                if (!lowestEnergies.TryGetValue(hash, out var neighbourEnergy))
                {
                    neighbourEnergy = int.MaxValue;
                }

                var newEnergy = currentEnergy + move.EnergyCost;
                if (newEnergy < neighbourEnergy)
                {
                    lowestEnergies[hash] = newEnergy;
                    queue.Enqueue(move.World, newEnergy);
                    previousWorld[hash] = (current, currentEnergy);
                }
            }
        }

        var currentHash = solvedState.GetHashCode();
        if (!lowestEnergies.TryGetValue(currentHash, out var result))
            return -1;

        return result;
    }


    static Board GetFinishedBoard(int boardSize)
    {
        switch (boardSize)
        {
            case 5:
                return new Board(
                    [
                        "#############",
                        "#...........#",
                        "###A#B#C#D###",
                        "  #A#B#C#D#",
                        "  #########"
                    ]);
            case 7:
                return new Board(
                    [
                        "#############",
                        "#...........#",
                        "###A#B#C#D###",
                        "  #A#B#C#D#",
                        "  #A#B#C#D#",
                        "  #A#B#C#D#",
                        "  #########"
                    ]);
            default: return null;
        }
    }

    static void Main()
    {
        var lines = new List<string>();
        string line;

        while (true)
        {
            line = Console.ReadLine();
            if (string.IsNullOrEmpty(line)) break;
            lines.Add(line);
        }

        Stopwatch sw = new Stopwatch();
        sw.Start();

        int result = Solve(lines);
        Console.WriteLine(result);
        sw.Stop();
        Console.WriteLine(sw.ElapsedMilliseconds);
    }
}