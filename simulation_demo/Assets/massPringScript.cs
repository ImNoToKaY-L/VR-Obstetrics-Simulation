using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MassSpringUnit
{
    private int vertex_num;
    private Vector3 pos;
    private Vector3 vel;
    private Vector3 force;
    private float velCoeff;
    private float mass;
    private float total_spring_len;

    // 后期要注意boundary的情况
    List<MassSpringUnit> neighbour_mass_spring_unit;
    List<Spring> neighbour_springs;

    public MassSpringUnit(int index, Vector3 pos)
    {
        this.vertex_num = index;
        this.pos = pos;
        this.neighbour_mass_spring_unit = new List<MassSpringUnit>();
        this.neighbour_springs = new List<Spring>();
        total_spring_len = 0f;
    }

    void Update()
    {

    }

    public bool AddNeighMassSpringUnit(MassSpringUnit mass_spring_unit)
    {
        // check duplicate mass
        foreach (MassSpringUnit neigh_mass in neighbour_mass_spring_unit)
        {
            if (neigh_mass.vertex_num == mass_spring_unit.vertex_num)
                return false;
        }
        neighbour_mass_spring_unit.Add(mass_spring_unit);
        return true;
    }

    public void AddNeighSpring(Spring spring)
    {

        neighbour_springs.Add(spring);
        total_spring_len += spring.GetLen();
    }

    public Vector3 GetPos()
    {
        return pos;
    }

    public float GetSpringLen()
    {
        return total_spring_len;
    }

    public void SetMass(float mass)
    {
        this.mass = mass;
    }
};

public class Spring
{
    private int index;
    private MassSpringUnit mass1;
    private MassSpringUnit mass2;
    private float spring_len;

    public Spring(int index, MassSpringUnit mass1, MassSpringUnit mass2)
    {
        this.index = index;
        this.mass1 = mass1;
        this.mass2 = mass2;
        spring_len = Calculate_len();
    }

    private float Calculate_len()
    {
        Vector3 pos1 = mass1.GetPos();
        Vector3 pos2 = mass2.GetPos();

        float dx = pos1.x - pos2.x;
        float dy = pos1.y - pos2.y;
        float dz = pos1.z - pos2.z;
        float len = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);

        return len;
    }

    public float GetLen()
    {
        return spring_len;
    }
}

public class massPringScript : MonoBehaviour
{

    public GameObject belly;
    private MassSpringUnit[] mass_spring_units;
    private Mesh mesh;
    ////////////////////////////////////
    /// Struct
    ////////////////////////////////////
    

    /////////////////////////////////////
    /// Functions
    /////////////////////////////////////

    // Start is called before the first frame update
    void Start()
    {
        mesh = belly.GetComponent<MeshFilter>().mesh;
        mass_spring_units = new MassSpringUnit [mesh.vertices.Length];
        InitMSModel();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void InitMSModel()
    {
        Vector3[] vertices = mesh.vertices;
        // initiate the mass spring list
        for(int index = 0; index < mesh.vertices.Length; index++)
        {
            mass_spring_units[index] = new MassSpringUnit(index, vertices[index]);
        }

        // find neighbour masses and springs
        int spring_index = 0;
        int[] triangles = mesh.triangles;
        for (int index = 0; index < mesh.triangles.Length; index+=3)
        {
            // Note: each 3 values of the triangles array store which vertices build this triangle 
            int ver1 = triangles[index];
            int ver2 = triangles[index + 1];
            int ver3 = triangles[index + 2];

            // add neighbour masses
            if (mass_spring_units[ver1].AddNeighMassSpringUnit(mass_spring_units[ver2]) 
                && mass_spring_units[ver2].AddNeighMassSpringUnit(mass_spring_units[ver1]))
            {
                // create springs between these vertices
                Spring spring12 = new Spring(spring_index, mass_spring_units[ver1], mass_spring_units[ver2]);
                spring_index++;
                mass_spring_units[ver1].AddNeighSpring(spring12);
                mass_spring_units[ver2].AddNeighSpring(spring12);
            }

            // add neighbour masses
            if (mass_spring_units[ver2].AddNeighMassSpringUnit(mass_spring_units[ver3]) 
                && mass_spring_units[ver3].AddNeighMassSpringUnit(mass_spring_units[ver2]))
            {
                // create springs between these vertices
                Spring spring23 = new Spring(spring_index, mass_spring_units[ver2], mass_spring_units[ver3]);
                spring_index++;
                mass_spring_units[ver2].AddNeighSpring(spring23);
                mass_spring_units[ver3].AddNeighSpring(spring23);
            }

            // add neighbour masses
            if (mass_spring_units[ver1].AddNeighMassSpringUnit(mass_spring_units[ver3]) 
                && mass_spring_units[ver3].AddNeighMassSpringUnit(mass_spring_units[ver1]))
            {
                // create springs between these vertices
                Spring spring13 = new Spring(spring_index, mass_spring_units[ver1], mass_spring_units[ver3]);
                spring_index++;
                mass_spring_units[ver1].AddNeighSpring(spring13);
                mass_spring_units[ver3].AddNeighSpring(spring13);
            }
        }

        // calculate the total spring length
        float total_spring_len = 0f;
        for (int index = 0; index < mesh.vertices.Length; index++)
        {
            total_spring_len += mass_spring_units[index].GetSpringLen();
        }

        // distribute the mass by spring length
        const float TOTAL_MASS = 20; // TODO adjust this param
        for (int index = 0; index < mesh.vertices.Length; index++)
        {
            float this_spring_len = mass_spring_units[index].GetSpringLen();
            float ratio = this_spring_len / total_spring_len;
            mass_spring_units[index].SetMass(ratio * TOTAL_MASS);
        }
    }

    public MassSpringUnit[] GetMassSpringUnits()
    {
        return mass_spring_units;
    }
}
