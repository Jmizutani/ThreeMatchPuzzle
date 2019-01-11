using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UniRx;
using System;

public class Board : MonoBehaviour
{

    private const int hSquareCount = 9;
    private const int vSquareCount = 9;
    private const int squareWidth = 40;
    private const int squareHight = 40;

    [SerializeField] private GameObject piece1;
    [SerializeField] private GameObject piece2;
    [SerializeField] private GameObject piece3;
    [SerializeField] private GameObject piece4;
    [SerializeField] private GameObject piece5;
    [SerializeField] private GameObject piece6;
    [SerializeField] private GameObject piecex1;
    [SerializeField] private GameObject piecex2;
    [SerializeField] private GameObject piecex3;
    [SerializeField] private GameObject piecex4;
    [SerializeField] private GameObject piecex5;
    [SerializeField] private GameObject piecex6;
    [SerializeField] private GameObject pieceb1;
    [SerializeField] private GameObject pieceb2;
    [SerializeField] private GameObject pieceb3;
    [SerializeField] private GameObject pieceb4;
    [SerializeField] private GameObject pieceb5;
    [SerializeField] private GameObject pieceb6;
    [SerializeField] private GameObject piece_R;
    [SerializeField] private GameObject arrow;
    [SerializeField] private GameObject stage;
    [SerializeField] private ParticleSystem sparkling;
    [SerializeField] private ParticleSystem explosion;
    [SerializeField] private ParticleSystem harrow;
    [SerializeField] private ParticleSystem varrow;

    private Piece[,] board = new Piece[hSquareCount, vSquareCount * 2];
    private bool[,] done = new bool[hSquareCount, vSquareCount]; //探索においてすでに通ったかどうか

    public bool Rmove = false;

    public void InitBoard()
    {
        for (int i = 0; i < hSquareCount; i++)
        {
            for (int j = 0; j < vSquareCount * 2; j++)
            {
                CreatePiece(new Vector3((i - hSquareCount / 2) * squareWidth, (j - vSquareCount / 2) * squareHight, 0f));
            }
        }
        while (HasMatch())
        {
            Deleteinit();
            Fillinit();
        }
    }

    public Piece GetNearestPiece(Vector3 input)
    {
        var min = float.MaxValue;
        Piece nearestPiece = null;

        foreach (var p in board)
        {
            var dist = Vector3.Distance(input, p.transform.position);
            if (dist < min)
            {
                nearestPiece = p;
                min = dist;
            }
        }
        return nearestPiece;
    }

    public void SwitchPiece(Piece p1, Piece p2)
    {
        var p1pos = p1.transform.position;
        p1.transform.DOMove(p2.transform.position, 0.2f);
        p2.transform.DOMove(p1pos, 0.2f);

        var p1boardpos = GetPieceBoardPos(p1);
        var p2boardpos = GetPieceBoardPos(p2);
        board[(int)p1boardpos.x / squareWidth + hSquareCount / 2, (int)p1boardpos.y / squareHight + vSquareCount / 2] = p2;
        board[(int)p2boardpos.x / squareWidth + hSquareCount / 2, (int)p2boardpos.y / squareHight + vSquareCount / 2] = p1;

        SetXpiece(p1);
        SetXpiece(p2);
        SetBpiece(p1);
        SetBpiece(p2);
        SetRpiece(p1);
        SetRpiece(p2);
        RDelete(p1, p2);
        RDelete(p2, p1);
    }

    public bool HasMatch()
    {
        foreach (var piece in board)
        {
            if (IsMatchPiece(piece))
                return true;
        }
        return false;
    }

    public bool HasBomb()
    {
        foreach (var p in board)
        {
            if (p.B == 1)
                return true;
        }
        return false;
    }

    public void DeleteMatchPiece()
    {
        SelectDeletePiece();

        foreach (var p in board)
        {
            if (p != null && p.deleteflag)
            {

                PlayEffect(p, sparkling);
                if (p.xVector == null)
                {
                    DOTween.ToAlpha(() => p.gameObject.GetComponent<Image>().color,
                    color => p.gameObject.GetComponent<Image>().color = color,
                    0f, 0.5f);
                    Observable.Timer(TimeSpan.FromSeconds(0.5f)).Subscribe(_ =>
                    {

                        Destroy(p.gameObject);
                        if (IsInboard(GetPieceBoardPos(p)))
                            GameController.score.Value += (int)(100 * GameController.rate);
                    }).AddTo(this);
                }
                else
                {
                    if (p.rflag)
                    {
                        Observable.Timer(TimeSpan.FromSeconds(0.5f)).Subscribe(_ =>
                        {
                            CreateRpiece(GetPieceBoardPos(p));
                            Destroy(p.gameObject);
                        }).AddTo(this);
                    }
                    else if (p.bflag)
                    {
                        Observable.Timer(TimeSpan.FromSeconds(0.5f)).Subscribe(_ =>
                        {
                            CreateBpiece(GetPieceBoardPos(p), p.Type);
                            Destroy(p.gameObject);
                        }).AddTo(this);
                    }
                    else if (p.xflag)
                    {
                        Observable.Timer(TimeSpan.FromSeconds(0.5f)).Subscribe(_ =>
                        {
                            CreateXpiece(GetPieceBoardPos(p), p.Type, p.Dir);
                            Destroy(p.gameObject);
                        }).AddTo(this);
                    }
                    else
                    {
                        p.transform.DOLocalMove((Vector3)p.xVector, 0.5f);
                        Observable.Timer(TimeSpan.FromSeconds(0.5f)).Subscribe(_ =>
                        {
                            Destroy(p.gameObject);
                        }).AddTo(this);
                    }
                }
            }
        }
    }

    public void FillPiece()
    {
        for (int i = 0; i < hSquareCount; i++)
        {
            for (int j = 0; j < vSquareCount * 2; j++)
            {
                FillPiece(new Vector3((i - hSquareCount / 2) * squareWidth, (j - vSquareCount / 2) * squareHight, 0f));
            }
        }
        foreach (var p in board)
        {
            SetXpiece(p);
            SetBpiece(p);
            SetRpiece(p);
        }
    }

    public Piece SelectNext(Piece p, GameController.FlickDirection dir)
    {
        switch (dir)
        {
            case GameController.FlickDirection.right:
                var pos = GetPieceBoardPos(p) + Vector3.right * squareWidth;
                return board[(int)pos.x / squareWidth + hSquareCount / 2, (int)pos.y / squareHight + vSquareCount / 2];
            case GameController.FlickDirection.left:
                pos = GetPieceBoardPos(p) + Vector3.left * squareWidth;
                return board[(int)pos.x / squareWidth + hSquareCount / 2, (int)pos.y / squareHight + vSquareCount / 2];
            case GameController.FlickDirection.up:
                pos = GetPieceBoardPos(p) + Vector3.up * squareHight;
                return board[(int)pos.x / squareWidth + hSquareCount / 2, (int)pos.y / squareHight + vSquareCount / 2];
            case GameController.FlickDirection.down:
                pos = GetPieceBoardPos(p) + Vector3.down * squareHight;
                return board[(int)pos.x / squareWidth + hSquareCount / 2, (int)pos.y / squareHight + vSquareCount / 2];
            default:
                return p;

        }
    }

    void CreatePiece(Vector3 pos)
    {
        Quaternion q = new Quaternion();
        q = Quaternion.identity;
        Piece p = null;
        switch (UnityEngine.Random.Range(1, 7))
        {
            case 1:
                p = Instantiate(piece1, pos, q).GetComponent<Piece>();
                break;
            case 2:
                p = Instantiate(piece2, pos, q).GetComponent<Piece>();
                break;
            case 3:
                p = Instantiate(piece3, pos, q).GetComponent<Piece>();
                break;
            case 4:
                p = Instantiate(piece4, pos, q).GetComponent<Piece>();
                break;
            case 5:
                p = Instantiate(piece5, pos, q).GetComponent<Piece>();
                break;
            case 6:
                p = Instantiate(piece6, pos, q).GetComponent<Piece>();
                break;
            default:
                break;
        }
        p.transform.SetParent(stage.transform, false);
        board[(int)pos.x / squareWidth + hSquareCount / 2, (int)pos.y / squareHight + vSquareCount / 2] = p;
    }

    void CreateXpiece(Vector3 pos, int type, bool dir)
    {
        Quaternion q = new Quaternion();
        q = Quaternion.identity;
        Piece p = null;
        switch (type)
        {
            case 1:
                p = Instantiate(piecex1, pos, q).GetComponent<Piece>();
                break;
            case 2:
                p = Instantiate(piecex2, pos, q).GetComponent<Piece>();
                break;
            case 3:
                p = Instantiate(piecex3, pos, q).GetComponent<Piece>();
                break;
            case 4:
                p = Instantiate(piecex4, pos, q).GetComponent<Piece>();
                break;
            case 5:
                p = Instantiate(piecex5, pos, q).GetComponent<Piece>();
                break;
            case 6:
                p = Instantiate(piecex6, pos, q).GetComponent<Piece>();
                break;
            default:
                break;
        }
        p.transform.SetParent(stage.transform, false);
        p.Dir = dir;
        board[(int)pos.x / squareWidth + hSquareCount / 2, (int)pos.y / squareHight + vSquareCount / 2] = p;
        CreateArrow(p, dir);
    }

    void CreateBpiece(Vector3 pos, int type)
    {
        Quaternion q = new Quaternion();
        q = Quaternion.identity;
        Piece p = null;
        switch (type)
        {
            case 1:
                p = Instantiate(pieceb1, pos, q).GetComponent<Piece>();
                break;
            case 2:
                p = Instantiate(pieceb2, pos, q).GetComponent<Piece>();
                break;
            case 3:
                p = Instantiate(pieceb3, pos, q).GetComponent<Piece>();
                break;
            case 4:
                p = Instantiate(pieceb4, pos, q).GetComponent<Piece>();
                break;
            case 5:
                p = Instantiate(pieceb5, pos, q).GetComponent<Piece>();
                break;
            case 6:
                p = Instantiate(pieceb6, pos, q).GetComponent<Piece>();
                break;
            default:
                break;
        }
        p.transform.SetParent(stage.transform, false);
        board[(int)pos.x / squareWidth + hSquareCount / 2, (int)pos.y / squareHight + vSquareCount / 2] = p;
    }

    void CreateRpiece(Vector3 pos)
    {
        Quaternion q = new Quaternion();
        q = Quaternion.identity;
        Piece p = Instantiate(piece_R, pos, q).GetComponent<Piece>();
        p.transform.SetParent(stage.transform, false);
        board[(int)pos.x / squareWidth + hSquareCount / 2, (int)pos.y / squareHight + vSquareCount / 2] = p;
    }

    void CreateArrow(Piece p, bool dir)
    {
        if (dir)
        {
            Quaternion q1 = new Quaternion();
            q1 = Quaternion.identity;
            Quaternion q2 = new Quaternion();
            q2 = Quaternion.Euler(0f, 0f, 180f);
            var arrow1 = Instantiate(arrow, new Vector3(0f, squareHight / 3, 0f), q1);
            arrow1.transform.SetParent(p.gameObject.transform, false);
            var arrow2 = Instantiate(arrow, new Vector3(0f, squareHight / 5, 0f), q1);
            arrow2.transform.SetParent(p.gameObject.transform, false);
            var arrow3 = Instantiate(arrow, new Vector3(0f, -squareHight / 3, 0f), q2);
            arrow3.transform.SetParent(p.gameObject.transform, false);
            var arrow4 = Instantiate(arrow, new Vector3(0f, -squareHight / 5, 0f), q2);
            arrow4.transform.SetParent(p.gameObject.transform, false);
        }
        else
        {
            Quaternion q1 = new Quaternion();
            q1 = Quaternion.Euler(0f, 0f, 90f);
            Quaternion q2 = new Quaternion();
            q2 = Quaternion.Euler(0f, 0f, 270f);
            var arrow1 = Instantiate(arrow, new Vector3(squareWidth / 3, 0f, 0f), q2);
            arrow1.transform.SetParent(p.gameObject.transform, false);
            var arrow2 = Instantiate(arrow, new Vector3(squareWidth / 5, 0f, 0f), q2);
            arrow2.transform.SetParent(p.gameObject.transform, false);
            var arrow3 = Instantiate(arrow, new Vector3(-squareWidth / 3, 0f, 0f), q1);
            arrow3.transform.SetParent(p.gameObject.transform, false);
            var arrow4 = Instantiate(arrow, new Vector3(-squareWidth / 5, 0f, 0f), q1);
            arrow4.transform.SetParent(p.gameObject.transform, false);
        }
    }

    void PlayEffect(Piece p, ParticleSystem particle)
    {
        var pos = GetPieceBoardPos(p) + Vector3.back;
        var effect = Instantiate(particle, pos, particle.transform.rotation);
        effect.transform.SetParent(stage.transform, false);
    }

    void SetXpiece(Piece piece)
    {
        if (IsXpiece(piece) > 0)
        {
            foreach (var p in board)
            {
                if (GetPieceIndex(p).y < vSquareCount && IsXpiece(p) > 0 && IsSameMatch(piece, p) && (p.X || p.B > 0))
                    return;
            }
            foreach (var p in board)
            {
                if (GetPieceIndex(p).y < vSquareCount && IsXpiece(p) > 0 && IsSameMatch(piece, p))
                {
                    p.xVector = GetPieceBoardPos(piece);
                    p.xflag = false;
                }
            }
            piece.xflag = IsXpiece(piece) > 0;
            piece.Dir = IsXpiece(piece) > 1;
        }

    }

    void SetBpiece(Piece piece)
    {
        if (IsBpiece(piece))
        {
            foreach (var p in board)
            {
                if (GetPieceIndex(p).y < vSquareCount && IsMatchPiece(p) && IsSameMatch(piece, p) && (p.X || p.B > 0))
                    return;
            }
            foreach (var p in board)
            {
                if (GetPieceIndex(p).y < vSquareCount && IsMatchPiece(p) && IsSameMatch(piece, p))
                {
                    p.xVector = GetPieceBoardPos(piece);
                    p.bflag = false;
                }
            }
            piece.bflag = IsBpiece(piece);
        }

    }

    void SetRpiece(Piece piece)
    {
        if (IsRpiece(piece))
        {
            foreach (var p in board)
            {
                if (GetPieceIndex(p).y < vSquareCount && IsRpiece(p) && IsSameMatch(piece, p) && (p.X || p.B > 0))
                    return;
            }
            foreach (var p in board)
            {
                if (GetPieceIndex(p).y < vSquareCount && IsRpiece(p) && IsSameMatch(piece, p))
                {
                    p.xVector = GetPieceBoardPos(piece);
                    p.rflag = false;
                }
            }
            piece.rflag = IsRpiece(piece);
        }

    }

    void SelectDeletePiece()
    {
        foreach (var p in board)
        {
            if (!p.deleteflag)
                p.deleteflag = IsMatchPiece(p);
        }
        while (HasSpecial())
        {
            XDelete();
            BDelete();
        }
        BCountDown();
        Rmove = false;
    }

    void XDelete()
    {
        foreach (var p in board)
        {
            if (p.X && p.deleteflag)
            {
                if (p.Dir)
                {
                    for (int i = 0; i < vSquareCount; i++)
                    {
                        board[(int)GetPieceBoardPos(p).x / squareWidth + hSquareCount / 2, i].deleteflag = true;
                    }
                    PlayEffect(p, varrow);
                }
                else
                {
                    for (int i = 0; i < hSquareCount; i++)
                    {
                        board[i, (int)GetPieceBoardPos(p).y / squareHight + vSquareCount / 2].deleteflag = true;
                    }
                    PlayEffect(p, harrow);
                }
                p.special = true;
            }
        }
    }

    void BDelete()
    {
        foreach (var p in board)
        {
            if (p.B == 1)
            {
                for (int i = -2; i < 3; i++)
                {
                    for (int j = Mathf.Abs(i) - 2; j < 2 - Mathf.Abs(i) + 1; j++)
                    {
                        int i_ = (int)GetPieceBoardPos(p).x / squareWidth + hSquareCount / 2 + i;
                        int j_ = (int)GetPieceBoardPos(p).y / squareHight + vSquareCount / 2 + j;
                        if (i_ >= 0 && i_ <= hSquareCount - 1 && j_ >= 0 && j_ <= vSquareCount - 1)
                            board[i_, j_].deleteflag = true;
                    }
                }
                p.B = 0;
                p.special = false;
                PlayEffect(p, explosion);
            }
            else if (p.B == 2 && p.deleteflag)
            {
                for (int i = -1; i < 2; i++)
                {
                    for (int j = -1; j < 2; j++)
                    {
                        int i_ = (int)GetPieceBoardPos(p).x / squareWidth + hSquareCount / 2 + i;
                        int j_ = (int)GetPieceBoardPos(p).y / squareHight + vSquareCount / 2 + j;
                        if (i_ >= 0 && i_ <= hSquareCount - 1 && j_ >= 0 && j_ <= vSquareCount - 1)
                            board[i_, j_].deleteflag = true;
                    }
                }
                p.special = true;
                PlayEffect(p, explosion);
            }
        }
    }

    void RDelete(Piece p1, Piece p2)
    {
        if (p1.Type == 7)
        {
            foreach (var p in board)
            {
                if (p.Type == p2.Type)
                {
                    p.deleteflag = true;
                }
            }
            p1.deleteflag = true;
            Rmove = true;
        }
    }

    void BCountDown()
    {
        foreach (var p in board)
        {
            if (p.B == 2 && p.deleteflag)
            {
                p.B = 1;
                p.deleteflag = false;
            }
        }
    }

    bool IsSameMatch(Piece p1, Piece p2)
    {
        done = new bool[hSquareCount, vSquareCount];
        return SameMatch(p1, p2);
    }

    bool SameMatch(Piece p1, Piece p2)
    {
        bool result = false;
        if (!IsMatchPiece(p1) || !IsMatchPiece(p2) || p1.Type != p2.Type)
            return false;

        if (p1 == p2)
            return true;

        Piece pup;
        Piece pright;
        Piece pdown;
        Piece pleft;
        int x = GetPieceIndex(p1).x;
        int y = GetPieceIndex(p1).y;
        done[x, y] = true;
        if (y <= vSquareCount - 2 && !done[x, y + 1])
        {
            pup = board[x, y + 1];
            if (p1.Type == pup.Type)
            {
                if (result = SameMatch(pup, p2))
                    return true;
            }
        }
        if (x <= hSquareCount - 2 && !done[x + 1, y])
        {
            pright = board[x + 1, y];
            if (p1.Type == pright.Type)
            {
                if (result = SameMatch(pright, p2))
                    return true;
            }
        }
        if (y >= 1 && !done[x, y - 1])
        {
            pdown = board[x, y - 1];
            if (p1.Type == pdown.Type)
            {
                if (result = SameMatch(pdown, p2))
                    return true;
            }
        }
        if (x >= 1 && !done[x - 1, y])
        {
            pleft = board[x - 1, y];
            if (p1.Type == pleft.Type)
            {
                if (result = SameMatch(pleft, p2))
                    return true;
            }
        }
        return false;
    }

    Vector2Int GetPieceIndex(Piece p)
    {
        for (int i = 0; i < hSquareCount; i++)
        {
            for (int j = 0; j < vSquareCount; j++)
            {
                if (board[i, j] == p)
                {
                    return new Vector2Int(i, j);
                }
            }
        }
        return Vector2Int.zero;
    }

    Vector3 GetPieceBoardPos(Piece p)
    {
        for (int i = 0; i < hSquareCount; i++)
        {
            for (int j = 0; j < vSquareCount * 2; j++)
            {
                if (board[i, j] == p)
                {
                    return new Vector3((i - hSquareCount / 2) * squareWidth, (j - vSquareCount / 2) * squareHight, 0f);
                }
            }
        }
        return Vector3.zero;
    }

    bool IsMatchPiece(Piece p)
    {
        var pos = GetPieceBoardPos(p);
        var type = p.Type;

        var vMatch = GetMatchCount(type, pos, Vector3.up) + GetMatchCount(type, pos, Vector3.down) + 1;
        var hMatch = GetMatchCount(type, pos, Vector3.right) + GetMatchCount(type, pos, Vector3.left) + 1;
        return vMatch >= GameController.matching || hMatch >= GameController.matching;
    }

    int IsXpiece(Piece p)
    {
        var pos = GetPieceBoardPos(p);
        var type = p.Type;

        var vMatch = GetMatchCount(type, pos, Vector3.up) + GetMatchCount(type, pos, Vector3.down) + 1;
        var hMatch = GetMatchCount(type, pos, Vector3.right) + GetMatchCount(type, pos, Vector3.left) + 1;
        if (hMatch >= GameController.matchX)
            return 1;
        else if (vMatch >= GameController.matchX)
            return 2;
        else
            return 0;
    }

    bool IsBpiece(Piece p)
    {
        var pos = GetPieceBoardPos(p);
        var type = p.Type;

        var vMatch = GetMatchCount(type, pos, Vector3.up) + GetMatchCount(type, pos, Vector3.down) + 1;
        var hMatch = GetMatchCount(type, pos, Vector3.right) + GetMatchCount(type, pos, Vector3.left) + 1;
        return vMatch >= GameController.matching && hMatch >= GameController.matching;
    }

    bool IsRpiece(Piece p)
    {
        var pos = GetPieceBoardPos(p);
        var type = p.Type;

        var vMatch = GetMatchCount(type, pos, Vector3.up) + GetMatchCount(type, pos, Vector3.down) + 1;
        var hMatch = GetMatchCount(type, pos, Vector3.right) + GetMatchCount(type, pos, Vector3.left) + 1;
        return vMatch >= GameController.matchR || hMatch >= GameController.matchR;
    }

    int GetMatchCount(int type, Vector3 piecepos, Vector3 dir)
    {
        var count = 0;
        while (true)
        {
            piecepos += dir * squareHight;
            if (IsInboard(piecepos) && board[(int)piecepos.x / squareWidth + hSquareCount / 2, (int)piecepos.y / squareHight + vSquareCount / 2].Type == type)
            {
                count++;
            }
            else
            {
                break;
            }
        }
        return count;
    }

    bool HasSpecial()
    {
        foreach (var p in board)
        {
            if (((p.X || p.B == 2) && p.deleteflag && !p.special) || p.B == 1)
                return true;
        }
        return false;
    }

    bool IsInboard(Vector3 pos)
    {
        return Mathf.Abs((int)pos.x) <= squareWidth * (hSquareCount / 2) && Mathf.Abs((int)pos.y) <= squareHight * (vSquareCount / 2);
    }

    bool IsIngame(Vector3 pos)
    {
        return Mathf.Abs((int)pos.x) <= squareWidth * (hSquareCount / 2) && (int)pos.y >= -squareHight * (vSquareCount / 2) && (int)pos.y <= squareHight * (vSquareCount * 3 / 2);
    }

    void FillPiece(Vector3 pos)
    {
        var p = board[(int)pos.x / squareWidth + hSquareCount / 2, (int)pos.y / squareHight + vSquareCount / 2];
        if (p != null && !p.deleteflag)
            return;

        var checkpos = pos + Vector3.up * squareHight;
        while (IsIngame(checkpos))
        {
            var checkPiece = board[(int)checkpos.x / squareWidth + hSquareCount / 2, (int)checkpos.y / squareHight + vSquareCount / 2];
            if (checkPiece != null && !checkPiece.deleteflag)
            {
                board[(int)pos.x / squareWidth + hSquareCount / 2, (int)pos.y / squareHight + vSquareCount / 2] = checkPiece;
                checkPiece.transform.DOLocalMove(pos, 0.2f);
                checkPiece.transform.DOScale(new Vector3(0.8f, 1f, 1f), 0.05f);
                Observable.Timer(TimeSpan.FromSeconds(0.2f)).Subscribe(_ =>
                {
                    checkPiece.transform.localScale = new Vector3(1f, 1f, 1f);
                    if (IsInboard(pos))
                        checkPiece.transform.DOPunchScale(new Vector3(0.3f, -0.3f, 0f), 1f, 5);
                }).AddTo(this);
                board[(int)checkpos.x / squareWidth + hSquareCount / 2, (int)checkpos.y / squareHight + vSquareCount / 2] = null;
                return;
            }
            checkpos += Vector3.up * squareHight;
        }
        CreatePiece(pos);
    }

    void Deleteinit()
    {
        foreach (var p in board)
        {
            p.deleteflag = IsMatchPiece(p);
        }

        foreach (var p in board)
        {
            if (p != null && p.deleteflag)
                Destroy(p.gameObject);
        }
    }

    void Fillinit()
    {
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 18; j++)
                Fillinit(new Vector3((i - hSquareCount / 2) * squareWidth, (j - vSquareCount / 2) * squareHight, 0f));
        }
    }

    void Fillinit(Vector3 pos)
    {
        var p = board[(int)pos.x / squareWidth + hSquareCount / 2, (int)pos.y / squareHight + vSquareCount / 2];
        if (p != null && !p.deleteflag)
            return;

        var checkpos = pos + Vector3.up * squareHight;
        while (IsIngame(checkpos))
        {
            var checkPiece = board[(int)checkpos.x / squareWidth + hSquareCount / 2, (int)checkpos.y / squareHight + vSquareCount / 2];
            if (checkPiece != null && !checkPiece.deleteflag)
            {
                board[(int)pos.x / squareWidth + hSquareCount / 2, (int)pos.y / squareHight + vSquareCount / 2] = checkPiece;
                checkPiece.transform.localPosition = pos;
                board[(int)checkpos.x / squareWidth + hSquareCount / 2, (int)checkpos.y / squareHight + vSquareCount / 2] = null;
                return;
            }
            checkpos += Vector3.up * squareHight;
        }
        CreatePiece(pos);
    }

}