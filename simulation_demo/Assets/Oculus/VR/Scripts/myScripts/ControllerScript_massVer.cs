using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ControllerScript_massVer : MonoBehaviour
{
    public Text help_text;
    public LineRenderer laser_line_renderer;
    public OVRPlayerController cam;
    public GameObject belly;
    public GameObject fetus_head;
    public Material surgeon_area;
    public Material non_surgeon_area;
    public Material fetus_head_area;
    public AudioSource sound_effect;
    public float touch_vibration_freq;
    public float press_distance;
    private Hashtable springs;
    private bool push;
    private int modify_index;
    private Vector3[] original_pos;
    private Vector3[] original_normals;
    private bool m_isOculusGo;


    // debug use!!
    private float last_ko;
    private float last_ks;
    public float ko;
    public float ks;


    // Start is called before the first frame update
    void Start()
    {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        push = false;
        press_distance = 0.02f;
        touch_vibration_freq = 0.1f;
        springs = new Hashtable();
        modify_index = -1;
        help_text.text = "No detection";
        original_pos = belly.GetComponent<MeshFilter>().sharedMesh.vertices;
        original_normals = belly.GetComponent<MeshFilter>().sharedMesh.normals;
        m_isOculusGo = (OVRPlugin.productName == "Oculus Go");
        InitSprings();

        // vars for changing param
        last_ko = ((spring)springs[0]).ko;
        last_ks = ((spring)springs[0]).ks;
        ko = last_ko;
        ks = last_ks;
        sw.Stop();
        System.TimeSpan ts = sw.Elapsed;
        print("Initial time for mass spring method: " + ts.TotalMilliseconds);
    }

    // Update is called once per frame
    void Update()
    {
        if(last_ko != ko || last_ks != ks)
        {
            foreach(spring spr in springs.Values)
            {
                spr.resetKoKs(ko, ks);
                last_ko = ko;
                last_ks = ks;
            }
        }

        // update direction 
        Vector2 dir = GetDirection();
        MoveToDir(dir);

        // check mouse or pointer position
        CheckIntersection();

        if (push)
        {
            ((spring)springs[modify_index]).cur_pos_ratio = 1;
        }

        // TODO RESET

        // update position
        ModifyModel();
    }

    void OnApplicationQuit()
    {
        belly.GetComponent<MeshFilter>().sharedMesh.vertices = original_pos;
        belly.GetComponent<MeshFilter>().sharedMesh.RecalculateNormals();
    }

    void InitSprings()
    {
        Mesh mesh = belly.GetComponent<MeshFilter>().sharedMesh;
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];
            spring spr = new spring();

            // get the position info from the vertex
            spr.setDistance(press_distance);
            spr.setID(i);
            spr.setOriPos(vertex);
            springs.Add(i, spr);
        }

        // get neighbour info
        for (int index = 0; index < triangles.Length; index += 3)
        {
            int ver1 = triangles[index];
            int ver2 = triangles[index + 1];
            int ver3 = triangles[index + 2];
            ((spring)springs[ver1]).addNeighbour(ver2); ((spring)springs[ver1]).addNeighbour(ver3);
            ((spring)springs[ver2]).addNeighbour(ver1); ((spring)springs[ver2]).addNeighbour(ver3);
            ((spring)springs[ver3]).addNeighbour(ver2); ((spring)springs[ver3]).addNeighbour(ver1);
        }
    }

    // Check the laser collision point with the belly mesh
    void CheckIntersection()
    {
        // Change the position of the controller

#if UNITY_EDITOR
        push = Input.GetMouseButton(0);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        transform.LookAt(ray.GetPoint(10000f));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
#else
        // Oculus version
        push = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger); // Get the current state of the trigger button
        RaycastHit hit;
        transform.rotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTrackedRemote);
        if (Physics.Raycast(transform.position, transform.forward, out hit))
#endif
        {
            // hit the surgeon area
            if (hit.collider != null && hit.collider.gameObject == belly)
            {
                if (!push) // not triggered
                {
                    help_text.text = "Not pushed";
                    
                    // change the line material
                    laser_line_renderer.sharedMaterial = surgeon_area;
                    if (sound_effect.isPlaying)
                        sound_effect.Stop();

                    // disable the vibration
                    OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);

                    // disable the sound in Oculus Go
                    if (m_isOculusGo)
                    {
                        
                        if (sound_effect.isPlaying)
                            sound_effect.Stop();
                    }
                }
                else //triggered
                {
                    // find the push vertex
                    int hit_vertex = findClosestVertex(hit);
                    help_text.text = "Pushed";

                    // avoid continuous push action
                    if (hit_vertex != modify_index)
                        ResetModel();
                    modify_index = hit_vertex;
                    
                    // enable the vibration
                    OVRInput.SetControllerVibration(touch_vibration_freq, touch_vibration_freq, OVRInput.Controller.RTouch);

                    // enable the sound in Oculus Go
                    if (m_isOculusGo)
                    {
                        if (!sound_effect.isPlaying)
                            sound_effect.Play();
                        sound_effect.volume = touch_vibration_freq;
                    }

                }
            }

            //set the laser end position
            laser_line_renderer.SetPosition(1, hit.point);
        }
        else // not in the surgeon area
        {
            help_text.text = "No intersection";
            laser_line_renderer.SetPosition(1, transform.forward * 10000);
            laser_line_renderer.sharedMaterial = non_surgeon_area;

            // disable the vibration
            OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);

            // disable the sound in Oculus Go
            if (m_isOculusGo)
            {

                if (sound_effect.isPlaying)
                    sound_effect.Stop();
            }
        }
        laser_line_renderer.SetPosition(0, transform.position);
    }

    // get the move direction from user
    Vector2 GetDirection()
    {
        // find the correct direction
        Vector2[] directions = new Vector2[]
        {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right
        };

#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.UpArrow))
            return directions[0];
        else if (Input.GetKey(KeyCode.DownArrow))
            return directions[1];
        else if (Input.GetKey(KeyCode.LeftArrow))
            return directions[2];
        else if (Input.GetKey(KeyCode.RightArrow))
            return directions[3];
        else
            return Vector2.zero;

#else

        if (m_isOculusGo) // Oculus Go
        {
            Vector2 coord = Vector2.zero;

            // check input
            if (!OVRInput.Get(OVRInput.Button.PrimaryTouchpad))
                return coord;
            
            coord = OVRInput.Get(OVRInput.Axis2D.PrimaryTouchpad, OVRInput.Controller.RTrackedRemote);
            Vector2 best_match_dir = Vector2.zero;
            float max = Mathf.NegativeInfinity;
            foreach (Vector2 vec in directions)
            {
                float dot_result = Vector2.Dot(vec, coord);
                if (dot_result > max)
                {
                    best_match_dir = vec;
                    max = dot_result;
                }
            }
            return best_match_dir;
        }
        else // Oculus Quest
        {
            if (OVRInput.Get(OVRInput.Button.SecondaryThumbstickUp))
                return directions[0];
            else if (OVRInput.Get(OVRInput.Button.SecondaryThumbstickDown))
                return directions[1];
            else if (OVRInput.Get(OVRInput.Button.SecondaryThumbstickLeft))
                return directions[2];
            else if (OVRInput.Get(OVRInput.Button.SecondaryThumbstickRight))
                return directions[3];
            else
                return Vector2.zero;
        }
#endif
    }

    // move the user and camera
    void MoveToDir(Vector2 dir)
    {
        float horizontalInput = dir.x;
        float verticalInput = dir.y;
        float movementSpeed = 20f;
        transform.position = transform.position + new Vector3(horizontalInput * movementSpeed * Time.deltaTime, 0, verticalInput * movementSpeed * Time.deltaTime);
        cam.transform.position = cam.transform.position + new Vector3(horizontalInput * movementSpeed * Time.deltaTime, 0, verticalInput * movementSpeed * Time.deltaTime);
        transform.rotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTrackedRemote);
        laser_line_renderer.SetPosition(1, transform.forward * 10000);
        laser_line_renderer.SetPosition(0, transform.position);
    }

    //  find the collision vertex
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
        else if (hit.collider.gameObject.GetComponent<SkinnedMeshRenderer>())
            return hit.collider.gameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh.triangles[hit.triangleIndex * 3 + max_index];
        else // error: no renderer
            return -1;
    }

    void ModifyModel()
    {
        System.Diagnostics.Stopwatch sw1 = new System.Diagnostics.Stopwatch();
        sw1.Start();
        Mesh mesh_to_modify = belly.GetComponent<MeshFilter>().sharedMesh;
        Vector3[] vertices = mesh_to_modify.vertices;
        int[] fetus_status_count = { 0, 0, 0 };
        Vector3 dir = transform.forward;
        if (modify_index != -1)
        {
            foreach (spring spr in springs.Values)
            {
                Vector3 ori_vec = vertices[spr.id];
                float distance_to_centre = Mathf.Pow(ori_vec.x - vertices[modify_index].x, 2) +
                                            Mathf.Pow(ori_vec.y - vertices[modify_index].y, 2) +
                                            Mathf.Pow(ori_vec.z - vertices[modify_index].z, 2);
                distance_to_centre = Mathf.Sqrt(distance_to_centre);
                if (distance_to_centre < 0.04f)
                {
                    spr.posifixing(springs, dir);
                    Vector3 vec = spr.calcposi(dir);
                    if (push && (vec.x != ori_vec.x || vec.y != ori_vec.y || vec.z != ori_vec.z)) // pushed, check fetus position
                    {
                        Vector3 push_point = belly.transform.TransformPoint(vec); // to world position
                        int fetus_status = CheckFetusPos(push_point, dir);
                        if (fetus_status == 1 || fetus_status == 2)
                        {
                            vec = new Vector3((vec.x + ori_vec.x) * 0.5f, (vec.y + ori_vec.y) * 0.5f, (vec.z + ori_vec.z) * 0.5f);
                        }
                        fetus_status_count[fetus_status]++;
                    }

                    vertices[spr.id] = vec;
                }

            }
        }
        
        if (push) // pushed, check fetus component info
        {
            // change the material according to the max region
            if (fetus_status_count.Max() == fetus_status_count[0])
            {
                help_text.text = "Not touched anything";
                laser_line_renderer.sharedMaterial = surgeon_area;
            }
            else if (fetus_status_count.Max() == fetus_status_count[1])
            {
                help_text.text = "Head is here";
                laser_line_renderer.sharedMaterial = fetus_head_area;
            }
            else
            {
                help_text.text = "Something is here";
                laser_line_renderer.sharedMaterial = fetus_head_area;
            }

            // calculate the ratio of fetus component in the pushed areas
            float ratio = (float)(fetus_status_count[1] + fetus_status_count[2]) / (float)fetus_status_count.Sum();

            // enable the vibration
            float freq = touch_vibration_freq + (1f - touch_vibration_freq) * ratio;
            OVRInput.SetControllerVibration(freq, freq, OVRInput.Controller.RTouch); // not in Oculus go

            // play the sound in Oculus Go
            if (m_isOculusGo)
            {
                if (!sound_effect.isPlaying)
                    sound_effect.Play();
                sound_effect.volume = freq;
            }
        }

        belly.GetComponent<MeshFilter>().sharedMesh.vertices = vertices;
        belly.GetComponent<MeshFilter>().sharedMesh.RecalculateNormals();

        sw1.Stop();
        System.TimeSpan ts1 = sw1.Elapsed;
        //if (push)
            //print("Time for push some point: " + ts1.TotalMilliseconds);

    }

    int CheckFetusPos(Vector3 pos, Vector3 dir)
    {
        //  Ray ray = new Ray(pos, new Vector3(0f, -1f, 0f)); // vertical dir
        Ray ray = new Ray(pos, dir); // norm dir


        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 13f))
        {
            if (hit.collider.tag == "Head")
            {
                return 1;
            }
            else
            {
                return 2;
            }
        }
        else
            return 0;
    }

    void ResetModel()
    {
        Mesh mesh_to_modify = belly.GetComponent<MeshFilter>().sharedMesh;
        mesh_to_modify.vertices = original_pos;
        mesh_to_modify.normals = original_normals;
    }
}



