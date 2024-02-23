namespace EESaga.Scripts.Maps;

using Entities;
using Entities.BattleEnemies;
using Entities.BattleParties;
using Godot;
using Interfaces;
using System.Collections.Generic;
using System.Linq;

public partial class PieceBattle : Node2D
{
    [Export] public double PieceMoveTime { get; set; } = 0.2;
    public IsometricTileMap TileMap { get; set; }
    public List<BattlePiece> Pieces { get; set; } = [];
    public List<BattleParty> Parties { get; set; } = [];
    public List<BattleEnemy> Enemies { get; set; } = [];
    public List<Obstacle> Obstacles { get; set; } = [];
    public List<Trap> Traps { get; set; } = [];

    public BattlePiece CurrentPiece { get; set; }

    public Dictionary<Vector2I, BattlePiece> PieceMap { get; set; } = [];

    private Room _room;
    private Node2D _enemies;
    private Node2D _parties;
    private Timer _pieceMoveTimer;
    private Camera2D _camera;

    private AStarGrid2D _astar = new()
    {
        CellSize = new Vector2I(1, 1),
        DefaultComputeHeuristic = AStarGrid2D.Heuristic.Manhattan,
        DefaultEstimateHeuristic = AStarGrid2D.Heuristic.Manhattan,
        DiagonalMode = AStarGrid2D.DiagonalModeEnum.Never,
    };
    private List<Vector2I> _pieceMovePath = [];
    private int _pieceMoveIndex = 0;

    public static PieceBattle Instance() => GD.Load<PackedScene>("res://Scenes/Maps/piece_battle.tscn").Instantiate<PieceBattle>();

    public override void _Ready()
    {
        TileMap = GetNode<IsometricTileMap>("TileMap");
        _enemies = GetNode<Node2D>("Enemies");
        _parties = GetNode<Node2D>("Parties");
        _pieceMoveTimer = GetNode<Timer>("PieceMoveTimer");
        _camera = GetNode<Camera2D>("Camera2D");

        _pieceMoveTimer.WaitTime = PieceMoveTime;
        _pieceMoveTimer.Timeout += OnPieceMoveTimerTimeout;

        #region test
        var tileMap = IsometricTileMap.Instance();
        for (var x = 0; x < 6; x++)
        {
            for (var y = 0; y < 6; y++)
            {
                tileMap.SetCell((int)Layer.Ground, new Vector2I(x, y), IsometricTileMap.TileSetId, IsometricTileMap.DefaultTileAtlas);
            }
        }
        Initialize(tileMap);
        AddEnemy(EnemyType.Slime);
        AddEnemy(EnemyType.Slime);
        AddEnemy(EnemyType.Slime);
        AddEnemy(EnemyType.Slime);
        AddEnemy(EnemyType.Slime);
        AddEnemy(EnemyType.Slime);
        var player = PlayerBattle.Instance();
        player.BattleCards = new UI.BattleCards()
        {
            DeckCards =
            [
                new CardInfo
                {
                    CardType = CardType.Attack,
                    CardName = "C_A_STRIKE",
                    CardDescription = "C_A_STRIKE_DESC",
                    CardCost = 1,
                    CardTarget = CardTarget.Enemy
                },
                new CardInfo
                {
                    CardType = CardType.Defense,
                    CardName = "C_D_DEFEND",
                    CardDescription = "C_D_DEFEND_DESC",
                    CardCost = 1,
                    CardTarget = CardTarget.Self
                },
                new CardInfo
                {
                    CardType = CardType.Special,
                    CardName = "C_S_STRUGGLE",
                    CardDescription = "C_S_STRUGGLE_DESC",
                    CardCost = 1,
                    CardTarget = CardTarget.Self
                },
                new CardInfo
                {
                    CardType = CardType.Item,
                    CardName = "C_I_ECS",
                    CardDescription = "C_I_ECS_DESC",
                    CardCost = 1,
                    CardTarget = CardTarget.Self
                },
                new CardInfo
                {
                    CardType = CardType.Attack,
                    CardName = "C_A_STRIKE",
                    CardDescription = "C_A_STRIKE_DESC",
                    CardCost = 1,
                    CardTarget = CardTarget.Enemy
                },
                new CardInfo
                {
                    CardType = CardType.Defense,
                    CardName = "C_D_DEFEND",
                    CardDescription = "C_D_DEFEND_DESC",
                    CardCost = 1,
                    CardTarget = CardTarget.Self
                },
                new CardInfo
                {
                    CardType = CardType.Special,
                    CardName = "C_S_STRUGGLE",
                    CardDescription = "C_S_STRUGGLE_DESC",
                    CardCost = 1,
                    CardTarget = CardTarget.Self
                },
                new CardInfo
                {
                    CardType = CardType.Item,
                    CardName = "C_I_ECS",
                    CardDescription = "C_I_ECS_DESC",
                    CardCost = 1,
                    CardTarget = CardTarget.Self
                }
            ],
            HandCards = [],
            DiscardCards =
            [
                new CardInfo
                {
                    CardType = CardType.Attack,
                    CardName = "C_A_STRIKE",
                    CardDescription = "C_A_STRIKE_DESC",
                    CardCost = 1,
                    CardTarget = CardTarget.Enemy
                },
                new CardInfo
                {
                    CardType = CardType.Defense,
                    CardName = "C_D_DEFEND",
                    CardDescription = "C_D_DEFEND_DESC",
                    CardCost = 1,
                    CardTarget = CardTarget.Self
                },
                new CardInfo
                {
                    CardType = CardType.Special,
                    CardName = "C_S_STRUGGLE",
                    CardDescription = "C_S_STRUGGLE_DESC",
                    CardCost = 1,
                    CardTarget = CardTarget.Self
                },
                new CardInfo
                {
                    CardType = CardType.Item,
                    CardName = "C_I_ECS",
                    CardDescription = "C_I_ECS_DESC",
                    CardCost = 1,
                    CardTarget = CardTarget.Self
                }
            ],
        };
        AddParty(player);
        #endregion
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left)
            {
                if (mouseEvent.Pressed)
                {
                    var cell = TileMap.SelectedCell;
                    if (cell != null &&
                        TileMap.IsDestination(cell.Value) &&
                        !CurrentPiece.IsMoving)
                    {
                        MoveCurrentPiece(cell.Value);
                    }
                }
            }
        }
    }

    public void Initialize(IsometricTileMap tileMap)
    {
        TileMap.CopyFrom(tileMap);
        foreach (var cell in TileMap.AvailableCells)
        {
            PieceMap[cell] = null;
        }
        var rectCenter = TileMap.GetUsedRect().GetCenter();
        _camera.GlobalPosition = TileMap.GlobalPosition +
            new Vector2(24 * (rectCenter.X - rectCenter.Y) + 12,
            12 * (rectCenter.X + rectCenter.Y));
        _camera.Enabled = true;
    }

    public void AddEnemy(EnemyType enemyType)
    {
        var enemy = enemyType switch
        {
            EnemyType.Slime => Slime.Instance(),
            _ => BattleEnemy.Instance(),
        };
        _enemies.AddChild(enemy);
        var rng = new RandomNumberGenerator();
        var cell = TileMap.AvailableCells[rng.RandiRange(0, TileMap.AvailableCells.Count - 1)];
        while (PieceMap[cell] != null)
        {
            cell = TileMap.AvailableCells[rng.RandiRange(0, TileMap.AvailableCells.Count - 1)];
        }
        enemy.GlobalPosition = PosForPiece(cell);
        PieceMap[cell] = enemy;
        Enemies.Add(enemy);
    }

    public void AddParty(PartyType partyType)
    {
        var party = partyType switch
        {
            PartyType.Player => PlayerBattle.Instance(),
            _ => BattleParty.Instance(),
        };
        _parties.AddChild(party);
        var rng = new RandomNumberGenerator();
        var cell = TileMap.AvailableCells[rng.RandiRange(0, TileMap.AvailableCells.Count - 1)];
        while (PieceMap[cell] != null)
        {
            cell = TileMap.AvailableCells[rng.RandiRange(0, TileMap.AvailableCells.Count - 1)];
        }
        party.GlobalPosition = PosForPiece(cell);
        PieceMap[cell] = party;
        Parties.Add(party);
    }

    public void AddParty(BattleParty party)
    {
        _parties.AddChild(party);
        var rng = new RandomNumberGenerator();
        var cell = TileMap.AvailableCells[rng.RandiRange(0, TileMap.AvailableCells.Count - 1)];
        while (PieceMap[cell] != null)
        {
            cell = TileMap.AvailableCells[rng.RandiRange(0, TileMap.AvailableCells.Count - 1)];
        }
        party.GlobalPosition = PosForPiece(cell);
        PieceMap[cell] = party;
        Parties.Add(party);
    }

    public void ShowAccessibleTiles(int range)
    {
        TileMap.ClearLayer((int)Layer.Mark);
        _astar.Clear();
        var src = TileMap.LocalToMap(CurrentPiece.GlobalPosition);
        var accessibleTiles = TileMap.GetAccessibleTiles(src, range);
        var primaryTiles = new List<Vector2I>();
        foreach (var tile in accessibleTiles)
        {
            if (PieceMap[tile] == null || tile == src)
            {
                primaryTiles.Add(tile);
            }
        }
        _astar.Region = TileMap.GetUsedRect();
        _astar.Update();
        var checkTiles = IsometricTileMap.Rect2IContains(_astar.Region);
        foreach (var tile in checkTiles)
        {
            if (!primaryTiles.Contains(tile))
            {
                _astar.SetPointSolid(tile);
            }
        }
        foreach (var tile in primaryTiles)
        {
            if (GetAStarPath(src, tile).Count <= range + 1 && GetAStarPath(src, tile).Count > 0)
            {
                TileMap.SetCell((int)Layer.Mark, tile, IsometricTileMap.TileSelectedId, IsometricTileMap.TileDestinationAtlas);
            }
        }
    }

    public void MoveCurrentPiece(Vector2I dst)
    {
        var src = TileMap.LocalToMap(CurrentPiece.GlobalPosition);
        var path = GetAStarPath(src, dst);
        if (path.Count > 1)
        {
            _pieceMovePath = path.Skip(1).ToList();
            _pieceMoveIndex = 0;
            CurrentPiece.IsMoving = true;
            _pieceMoveTimer.Start();
        }
    }

    public void EndBattle()
    {
    }

    private Vector2 PosForPiece(Vector2I coord) => TileMap.MapToLocal(coord) - new Vector2(0, 6);

    private List<Vector2I> GetAStarPath(Vector2I src, Vector2I dst) => new(_astar.GetIdPath(src, dst));

    private void OnPieceMoveTimerTimeout()
    {
        if (_pieceMovePath != null && _pieceMovePath.Count > 0)
        {
            var tween = CreateTween();
            tween.TweenProperty(CurrentPiece, "global_position",
                PosForPiece(_pieceMovePath[_pieceMoveIndex]), PieceMoveTime);
            CurrentPiece.FlipH = PosForPiece(_pieceMovePath[_pieceMoveIndex]).X - CurrentPiece.GlobalPosition.X < 0;
            if (_pieceMoveIndex == _pieceMovePath.Count - 1)
            {
                _pieceMovePath = [];
                _pieceMoveIndex = 0;
                CurrentPiece.IsMoving = false;
                _pieceMoveTimer.Stop();
            }
            else
            {
                _pieceMoveIndex++;
            }
        }
    }
}