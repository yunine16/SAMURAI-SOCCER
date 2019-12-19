﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class BallControler : MonoBehaviour
{
    private Rigidbody rb;
    private static readonly float sqrt3 = Mathf.Sqrt(3);
    private static readonly float sqrt2 = Mathf.Sqrt(2);
    private static readonly float gravity = 9.8f;

    public bool last_touch;
    public GameObject owner;
    public delegate void GoalEventHandler(object sender, GoalEventArgs e);
    public delegate void OutBallEventHandler(object sender, OutBallEventArgs e);
    public delegate void PassEventHandler(object sender, PassEventArgs e);
    public delegate void StealEventHandler(object sender, StealEventArgs e);
    public delegate void DribbleEventHandler(object sender, DribbleEventArgs e);
    public delegate void TrapEventHandler(object sender, TrapEventArgs e);

    public event GoalEventHandler Goal;
    public event OutBallEventHandler OutBall;
    public event PassEventHandler PassSend;
    public event StealEventHandler StealBall;
    public event DribbleEventHandler DribbleKick;
    public event TrapEventHandler Trapping;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }


    /// <summary>
    /// ドリブルっぽいもの。StartCorutineじゃなくてイテレーターで操作してほしい。
    /// </summary>
    /// <returns></returns>
    public IEnumerator Dribble(PersonalStatus self)
    {
        last_touch = self.ally;
        rb.AddForce(5 * new Vector3(-Mathf.Sin(owner.transform.rotation.eulerAngles.y * Mathf.Deg2Rad), 0, Mathf.Cos(owner.transform.rotation.eulerAngles.y * Mathf.Deg2Rad)), ForceMode.Impulse);
        var e = new DribbleEventArgs();
        e.owner = owner;
        OnDribble(e);
        yield return null;
        for(int i = 0; i < 19; i++)
        {
            yield return null;
        }
        Vector3 pos = transform.position;
        for(int i = 0; i < 20; i++)
        {
            pos.x = Mathf.Lerp(pos.x, owner.transform.position.x, i / 20.0f);
            pos.z = Mathf.Lerp(pos.z, owner.transform.position.z, i / 20.0f);
            transform.position = pos;

            yield return null;
        }

    }

    private void OnDribble(DribbleEventArgs e)
    {
        DribbleKick?.Invoke(this, e);
    }

    /// <summary>
    /// ボールを盗むときの関数
    /// </summary>
    /// <param name="holder">ボールを持っている人の能力値</param>
    /// <param name="tryer">ボールを奪う人の能力値</param>
    /// <param name="self">ボール奪う人</param>
    public void Steal(GameObject self, PersonalStatus holder = default, PersonalStatus tryer = default)
    {
        if(SuccessSteal(holder, tryer))
        {
            owner = self;
            var e = new StealEventArgs();
            e.stealer = self;
            OnSteal(e);
        }
    }

    private bool SuccessSteal(PersonalStatus holder, PersonalStatus tryer)
    {
        //TODO: 盗む判定
        return true;
    }

    private void OnSteal(StealEventArgs e)
    {
        StealBall?.Invoke(this, e);
    }


    /// <summary>
    /// トラップするとき呼ぶ関数
    /// </summary>
    /// <param name="self">能力値</param>
    public void Trap(GameObject recever,PersonalStatus self = default)
    {
        if (SuccessTrap(self))
        {
            rb.velocity = Vector3.zero;
            owner = recever;
            var e = new TrapEventArgs();
            e.owner = recever;
            OnTrap(e);
        }
    }

    private void OnTrap(TrapEventArgs e)
    {
        Trapping?.Invoke(this, e);
    }

    /// <summary>
    /// トラップが成功するかの判定
    /// </summary>
    /// <param name="self">能力値</param>
    /// <returns>成功のときtrue</returns>
    private bool SuccessTrap(PersonalStatus self)
    {
        //ToDo: トラップ判定
        return true;
    }

    /// <summary>
    /// パスをするとき呼ぶ関数
    /// </summary>
    /// <param name="sender">パス出す人</param>
    /// <param name="recever">パス受け取る人</param>
    /// <param name="height">パスの高さ</param>
    /// <param name="self">自分の能力</param>
    public void Pass(Vector2 sender, Vector2 recever, PassHeight height = PassHeight.Middle, PersonalStatus self = default)
    {
        if(self == default)
        {
            self.power = 30;
        }
        switch (height)
        {
            case PassHeight.Low:    CalcLowPass(sender, recever, self); break;
            case PassHeight.Middle: CalcMiddlePass(sender, recever, self); break;
            case PassHeight.High:   CalcHighPass(sender, recever, self); break;
        }

        var e = new PassEventArgs();
        e.sender = sender;
        e.recever = recever;
        e.height = height;
        OnPass(e);
    }

    private void CalcLowPass(Vector2 sender, Vector2 recever, PersonalStatus self)
    {
        Vector2 dest = (recever - sender).normalized;

        rb.AddForceAtPosition(self.power * dest, 0.3f * new Vector3(-dest.x / 2, 0.4f, -dest.y / 2), ForceMode.Impulse);
    }

    private void CalcMiddlePass(Vector2 sender, Vector2 recever, PersonalStatus self)
    {
        Vector2 dest = (recever - sender);
        float distance = dest.magnitude;
        float power = Mathf.Sqrt(3 * gravity * distance) / 2;
        if(power > self.power)
        {
            power = self.power;
        }
        dest = dest.normalized;
        rb.AddForceAtPosition(power * new Vector3(2 / sqrt3 * dest.x, 1.0f / sqrt3, 2 / sqrt3 * dest.y), 0.3f * new Vector3(-2 / sqrt3 * dest.x, -1.0f / sqrt3, -2 / sqrt3 * dest.y), ForceMode.Impulse);
    }

    private void CalcHighPass(Vector2 sender, Vector2 recever, PersonalStatus self)
    {
        Vector3 dest = recever - sender;
        float distance = dest.magnitude;
        float power = Mathf.Sqrt(gravity * distance);
        if (power > self.power)
        {
            power = self.power;
        }
        dest = dest.normalized;
        rb.AddForceAtPosition(power * new Vector3(dest.x / sqrt2, 1.0f / sqrt2, dest.z / sqrt2), 0.3f * new Vector3(-dest.x / sqrt2, -1.0f / sqrt2, -dest.z / sqrt2), ForceMode.Impulse);

    }

    private void OnPass(PassEventArgs e)
    {
        PassSend?.Invoke(this, e);
    }

    /// <summary>
    /// シュートを撃つ関数
    /// </summary>
    /// <param name="sender">シュート撃つ人</param>
    /// <param name="self">能力値</param>
    public void Shoot(GameObject sender, PersonalStatus self = default)
    {
        if(self == default)
        {
            self.power = 30;
        }

        Vector3 dest;
        if (self.ally)
        {
            dest = (new Vector3(Random.Range(-10, 10), Random.Range(0.0f, 2.0f), 50) - sender.transform.position).normalized;
        }
        else
        {
            dest = (new Vector3(Random.Range(-10, 10), Random.Range(0.0f, 2.0f), -50) - sender.transform.position).normalized;
        }

        rb.AddForceAtPosition(self.power * dest, 0.3f * new Vector3(-dest.x, -dest.y, dest.z),ForceMode.Impulse);
        Debug.Log(self.power * dest);
    }

    /// <summary>
    /// 特殊なイベント発生
    /// </summary>
    /// <param name="collision"></param>
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Goal")
        {
            var e = new GoalEventArgs();
            e.Ally = last_touch;
            OnGoal(e);
        }else if(collision.gameObject.tag == "OutWall")
        {
            var e = new OutBallEventArgs();
            e.Ally = last_touch;
            e.Point = new Vector2(transform.position.x, transform.position.z);
            OnOutBall(e);
        }

    }

    private void OnGoal(GoalEventArgs e)
    {
        Goal?.Invoke(this, e);
    }

    private void OnOutBall(OutBallEventArgs e)
    {
        OutBall?.Invoke(this, e);
    }

}

public enum PassHeight
{
    Low,
    Middle,
    High
}

public class GoalEventArgs : EventArgs {
    public bool Ally;
}

public class OutBallEventArgs : EventArgs {
    public bool Ally;
    public Vector2 Point;
}

public class PassEventArgs : EventArgs {
    public Vector2 sender;
    public Vector2 recever;
    public PassHeight height;
}


public class StealEventArgs : EventArgs
{
    public GameObject stealer;
}

public class DribbleEventArgs : EventArgs
{
    public GameObject owner;
}

public class TrapEventArgs : EventArgs
{
    public GameObject owner;
}