using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MassSpringScript : MonoBehaviour
{
    Hashtable springs;
    private float press_distance;
    Vector3[] ori_vecs;
    public LineRenderer line;

    // Start is called before the first frame update
    void Start()
    {
        springs = new Hashtable();
        ori_vecs = this.GetComponent<MeshFilter>().sharedMesh.vertices;
        press_distance = 0.01f;
        InitSprings();
    }

    // Update is called once per frame
    void Update()
    {


#if UNITY_EDITOR
        int push_ver = GetMousePos();

        if (push_ver != -1)
        {
            ((spring)springs[push_ver]).curposi = 1;
        }
        //reset the vertices  
        if (Input.GetMouseButton(2))
        {
               
            this.GetComponent<MeshFilter>().sharedMesh.vertices = ori_vecs;
            this.GetComponent<MeshFilter>().sharedMesh.RecalculateNormals();
        }
        else
#else
        // TODO ADD RESET FOR OCULUS GO AND OCULUS QUEST
        int push_ver = GetControllerPos();

        if (push_ver != -1)
        {
            ((spring)springs[push_ver]).curposi = 1;
        }
#endif
        // update the vertex pos

        {
            
            Vector3[] vertices = this.GetComponent<MeshFilter>().sharedMesh.vertices;
            foreach (spring spr in springs.Values)
            {
                spr.posifixing(springs);
                float[] pos = spring.calcposi(spr.getposi());
                Vector3 vec = new Vector3(pos[0], pos[1], pos[2]);
                vertices[spr.id] = vec;
            }
            this.GetComponent<MeshFilter>().sharedMesh.vertices = vertices;
            this.GetComponent<MeshFilter>().sharedMesh.RecalculateNormals();
        }

        
    }

    void OnApplicationQuit()
    {
        this.GetComponent<MeshFilter>().sharedMesh.vertices = ori_vecs;
        this.GetComponent<MeshFilter>().sharedMesh.RecalculateNormals();
    }

    int GetMousePos()
    {
        if(Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider != null)
                {
                    line.SetPosition(1, hit.point);
                    return findClosestVertex(hit);
                }
            }
        }
        return -1;
    }

    int GetControllerPos()
    {
        var trans = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTrackedRemote);
        // Get the current state of the trigger button
        if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger))
        { 
            RaycastHit hit;
            
            if (Physics.Raycast(line.gameObject.transform.position, line.gameObject.transform.forward, out hit))
            {
                if (hit.collider != null)
                {
                    line.SetPosition(1, hit.point);
                    return findClosestVertex(hit);
                }
            }
        }
        
        return -1;
    }

    int findClosestVertex(RaycastHit hit)
    {
        Vector3 bary_coor = hit.barycentricCoordinate;
        float[] bary_coors = { bary_coor.x, bary_coor.y, bary_coor.z };
        float max = -1000;
        int max_index = -1;
        for (int i = 0; i < 3; i++)
        {
            float coor = bary_coors[i];
            if (coor > max)
            {
                max = coor;
                max_index = i;
            }
        }
        if (hit.collider.gameObject.GetComponent<MeshFilter>())
            return hit.collider.gameObject.GetComponent<MeshFilter>().sharedMesh.triangles[hit.triangleIndex * 3 + max_index];
        else // error
            return -1;
    }

    void InitSprings()
    {
        Mesh mesh = this.GetComponent<MeshFilter>().sharedMesh;
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;
        for(int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];
            Vector3 normal = mesh.normals[i];
            spring spr = new spring();

            // get the position info from the vertex
            float[] posarr = new float[6];
            posarr[0] = vertex.x; posarr[3] = vertex.x - normal.x * press_distance;
            posarr[1] = vertex.y; posarr[4] = vertex.y - normal.y * press_distance;
            posarr[2] = vertex.z; posarr[5] = vertex.z - normal.z * press_distance;
            spr.setid(i);
            spr.setposi(posarr);
            springs.Add(i, spr);
        }

        // get neighbour info
        for (int index = 0; index < triangles.Length; index += 3)
        {
            int ver1 = triangles[index];
            int ver2 = triangles[index + 1];
            int ver3 = triangles[index + 2];
            ((spring)springs[ver1]).setneighbourhood(ver2); ((spring)springs[ver1]).setneighbourhood(ver3);
            ((spring)springs[ver2]).setneighbourhood(ver1); ((spring)springs[ver2]).setneighbourhood(ver3);
            ((spring)springs[ver3]).setneighbourhood(ver2); ((spring)springs[ver3]).setneighbourhood(ver1);
        }
    }
}

class spring
{
    public int id;
    private float stx, sty, stz, edx, edy, edz;
    public bool isfollow = true;
    public bool isfix = false;
    public float ko = 2f;
    public float ks = 0.01f;
    public float curposi = 0f;
    private HashSet<int> neighbour;
    private HashSet<int> influence_region;

    public spring()
    {
        neighbour = new HashSet<int>();
        neighbour = new HashSet<int>();
    }

    public void reset_koks(float ko, float ks)
    {
        this.ko = ko;
        this.ks = ks;
    }

    public void setid(int id)
    {
        this.id = id;
    }
    public void setneighbourhood(int neighboor_index)
    {
        this.neighbour.Add(neighboor_index);
    }
    public void setposi(float[] arr)
    {
        this.stx = arr[0];
        this.sty = arr[1];
        this.stz = arr[2];
        this.edx = arr[3];
        this.edy = arr[4];
        this.edz = arr[5];
    }
    public float[] getposi()
    {
        float[] i = new float[7];
        i[0] = this.stx;
        i[1] = this.sty;
        i[2] = this.stz;
        i[3] = this.edx;
        i[4] = this.edy;
        i[5] = this.edz;
        i[6] = this.curposi;
        return i;
    }

    public HashSet<int> getneighbours()
    {
        return neighbour;
    }

    public static float[] calcposi(float[] op_posi)
    {
        float[] cur_posi = new float[3];
        cur_posi[0] = op_posi[0] + (op_posi[3] - op_posi[0]) * op_posi[6];
        cur_posi[1] = op_posi[1] + (op_posi[4] - op_posi[1]) * op_posi[6];
        cur_posi[2] = op_posi[2] + (op_posi[5] - op_posi[2]) * op_posi[6];
        return cur_posi;
    }

    public float calc_prefix(spring A, spring B)
    {
        //A for local point, B for neighboor point
        float[] _a = A.getposi();
        float[] _b = B.getposi();

        float[] a = calcposi(_a);
        float[] b = calcposi(_b);

        float[] vera = new float[3];
        float[] verb = new float[3];
        vera[0] = _a[3] - _a[0];
        vera[1] = _a[4] - _a[1];
        vera[2] = _a[5] - _a[2];
        verb[0] = b[0] - a[0];
        verb[1] = b[1] - a[1];
        verb[2] = b[2] - a[2];

        float lena = Mathf.Sqrt(vera[0] * vera[0] + vera[1] * vera[1] + vera[2] * vera[2]);
        float lenb = Mathf.Sqrt(verb[0] * verb[0] + verb[1] * verb[1] + verb[2] * verb[2]);

        float prefix = (vera[0] * verb[0] + vera[1] * verb[1] + vera[2] * verb[2]) / lena;
        return prefix;
    }

    public void posifixing(Hashtable spring_table)
    {
        if (this.isfollow && !this.isfix)
        {
            float mpf = -this.curposi * this.ks;
            foreach (int i in this.neighbour)
            {
                //Console.WriteLine("({0,2}", i);
                mpf += calc_prefix((spring)spring_table[this.id], (spring)spring_table[i]) * this.ko;
            }
            //Console.WriteLine("");
            this.curposi += mpf;
            if (this.curposi > 1)
            {
                this.curposi = 1;
            }
            this.curposi -= 0.023f;
            if (this.curposi < 0)
            {
                this.curposi = 0;
            }
        }
    }
}
