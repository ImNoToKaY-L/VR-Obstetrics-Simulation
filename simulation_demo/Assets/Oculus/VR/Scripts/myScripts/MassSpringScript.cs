using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class spring
{
    public int id;
    private Vector3 ori_pos;
    private float push_distance;
    public float ko = 2f;
    public float ks = 0.01f;
    public float cur_pos_ratio = 0f;
    private HashSet<int> neighbour;
    private HashSet<int> influence_region;

    public spring()
    {
        neighbour = new HashSet<int>();
        neighbour = new HashSet<int>();
    }

    public void resetKoKs(float ko, float ks)
    {
        this.ko = ko;
        this.ks = ks;
    }

    public void setID(int id)
    {
        this.id = id;
    }
    
    public void setDistance(float dis)
    {
        this.push_distance = dis;
    }
    public void addNeighbour(int neighboor_index)
    {
        this.neighbour.Add(neighboor_index);
    }
    public void setOriPos(Vector3 pos)
    {
        this.ori_pos = pos;
    }
    public float getPosRatio()
    {
        return cur_pos_ratio;
    }
    public Vector3 getOriPos()
    {
        return ori_pos;
    }

    public HashSet<int> getneighbours()
    {
        return neighbour;
    }

    public Vector3 calcposi(Vector3 dir)
    {
        Vector3 cur_pos = new Vector3(ori_pos.x + dir.x * push_distance * cur_pos_ratio,
                                    ori_pos.y + dir.y * push_distance * cur_pos_ratio,
                                    ori_pos.z + dir.z * push_distance * cur_pos_ratio);
        return cur_pos;
    }

    public float calc_prefix(spring A, spring B, Vector3 dir)
    {
        // A for local point, B for neighboor point
        // get position information
        Vector3 a_oripos = A.getOriPos();
        Vector3 a_endpos = new Vector3(a_oripos.x + dir.x * A.push_distance, a_oripos.y + dir.y * A.push_distance, a_oripos.z + dir.z * A.push_distance);

        // update the position
        Vector3 a = A.calcposi(dir);
        Vector3 b = B.calcposi(dir);

        // calculate the spring length
        float[] vera = new float[3];
        float[] verb = new float[3];
        vera[0] = a_endpos.x - a_oripos.x;
        vera[1] = a_endpos.y - a_oripos.y;
        vera[2] = a_endpos.z - a_oripos.z;
        verb[0] = b[0] - a[0];
        verb[1] = b[1] - a[1];
        verb[2] = b[2] - a[2];

        float lena = Mathf.Sqrt(vera[0] * vera[0] + vera[1] * vera[1] + vera[2] * vera[2]);

        // vector components on the vertical spring
        float prefix = (vera[0] * verb[0] + vera[1] * verb[1] + vera[2] * verb[2]) / lena;
        return prefix;
    }

    public void posifixing(Hashtable spring_table, Vector3 dir)
    {
        float mpf = -this.cur_pos_ratio * this.ks;
        foreach (int i in this.neighbour)
        {
            //Console.WriteLine("({0,2}", i);
            mpf += calc_prefix((spring)spring_table[this.id], (spring)spring_table[i], dir) * this.ko;
        }
        //Console.WriteLine("");
        this.cur_pos_ratio += mpf;
        if (this.cur_pos_ratio > 1)
        {
            this.cur_pos_ratio = 1;
        }
        this.cur_pos_ratio -= 0.023f;

        if (this.cur_pos_ratio < 0)
        {
            this.cur_pos_ratio = 0;
        }
    }
}
