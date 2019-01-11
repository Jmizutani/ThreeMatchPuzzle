using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour
{

    public bool deleteflag;
    public bool xflag;  //xピースになるかどうか
    public Vector3? xVector; //xピースに吸収される位置
    public bool bflag; //bピースになるかどうか
    public bool rflag; //rピースになるかどうか
    public bool special;  //特殊ピースの効果が発動したかどうか

    private int type;
    [SerializeField] private bool isx;  //xピースかどうか
    [SerializeField] private bool direction; //false:horizontal true:vertical xピースの消す方向
    [SerializeField] private int isb;  //bピースかどうか 0:違う 1:あと一回爆発 2:あと二回爆発

    private void Awake()
    {
        deleteflag = false;
        xflag = false;
        xVector = null;
    }

    public int Type
    {
        get
        {
            switch (transform.tag)
            {
                case "piece1":
                    type = 1;
                    break;
                case "piece2":
                    type = 2;
                    break;
                case "piece3":
                    type = 3;
                    break;
                case "piece4":
                    type = 4;
                    break;
                case "piece5":
                    type = 5;
                    break;
                case "piece6":
                    type = 6;
                    break;
                case "pieceR":
                    type = 7;
                    break;
                default:
                    type = 8;
                    break;
            }

            return type;
        }
    }

    public bool X
    {
        get { return this.isx; }
    }

    public int B
    {
        get { return this.isb; }
        set { this.isb = value; }
    }

    public bool Dir
    {
        get { return this.direction; }
        set { this.direction = value; }
    }
}