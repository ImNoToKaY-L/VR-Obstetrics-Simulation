using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ControllerScript : MonoBehaviour
{
    public Text help_text;
    public LineRenderer laser_line_renderer;
    public OVRPlayerController cam;
    public GameObject belly;
    public GameObject fetus_head;
    public LineRenderer fetus_line;
    public Material surgeon_area;
    public Material non_surgeon_area;
    public Material fetus_head_area;
    public AudioSource sound_effect;
    private bool push;
    private List<int> path;
    private int modify_index;
    private Vector3[] original_pos;
    private Vector3[] original_normals;


    // Gaussian
    float inv_denominator;
    public float theta = 5f;
    public float max_dz = 0.02f;
    public float affect_region = 0.04f;

    // Start is called before the first frame update
    void Start()
    {
        push = false;
        modify_index = -1;
        path = new List<int>();
        help_text.text = "No detection";
        original_pos = belly.GetComponent<MeshFilter>().sharedMesh.vertices;
        original_normals = belly.GetComponent<MeshFilter>().sharedMesh.normals;
        inv_denominator = 1 / (2 * Mathf.PI * theta * theta);
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_EDITOR
        if (Input.anyKey)
        {
            Vector2 dir = GetDirection();
            ChangePosition(dir);
        }
#else
        if (OVRInput.Get(OVRInput.Button.PrimaryTouchpad))
        {
            Vector2 dir = GetDirection();
            ChangePosition(dir);
        }
#endif
        CheckIntersection();

        if (modify_index != -1)
        {
            ModifyModel();
            modify_index = -1;
        }
        else
        {
            ResetModel();
        }
    }

    // Check the laser collision point with the belly mesh
    void CheckIntersection()
    {
        // TODO change to oculus for debug

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
                    int hit_vertex = findClosestVertex(hit);
                    // help_text.text = "No cut path, hit at vertex" + hit_vertex;
                    help_text.text = "Not pushed";
                    path = new List<int>();
                    modify_index = -1;
                    laser_line_renderer.sharedMaterial = surgeon_area;
                    if (sound_effect.isPlaying)
                        sound_effect.Stop();
                }
                else //triggered
                {
                    int hit_vertex = findClosestVertex(hit);
                    if (!path.Contains(hit_vertex))
                        path.Add(hit_vertex);
                    string path_str = "";
                    path.ForEach(num => path_str += (num.ToString() + ", "));
                    // help_text.text = "Cut path:" + path_str;
                    // help_text.text = "push at vertex " + hit_vertex;
                    help_text.text = "Pushed";
                    if (hit_vertex != modify_index)
                        ResetModel();
                    modify_index = hit_vertex;
                }
            }

            //set the laser end position
            laser_line_renderer.SetPosition(1, hit.point);
        }
        else // not in the surgeon area
        {
            help_text.text = "No intersection";
            path = new List<int>();
            laser_line_renderer.SetPosition(1, transform.forward * 10000);
            laser_line_renderer.sharedMaterial = non_surgeon_area;
            if (sound_effect.isPlaying)
                sound_effect.Stop();
        }
        laser_line_renderer.SetPosition(0, transform.position);
    }

    // get the direction from the controller pad
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
        Vector2 coord = OVRInput.Get(OVRInput.Axis2D.PrimaryTouchpad, OVRInput.Controller.RTrackedRemote);
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
#endif
    }

    // move the user and camera
    void ChangePosition(Vector2 dir)
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

    float Gaussian_2d(float dx, float dy)
    {
        float e_term = Mathf.Exp(-(dx * dx + dy * dy) / (2 * theta * theta));
        float ret = inv_denominator * e_term;
        return ret;
    }

    void ModifyModel()
    {
        Mesh mesh_to_modify = belly.GetComponent<MeshFilter>().sharedMesh;
        Vector3[] vertices = mesh_to_modify.vertices;
        Vector3[] normals = mesh_to_modify.normals;
        Vector3 centre_point = vertices[modify_index];

        Dictionary<int, float> change_vertices = new Dictionary<int, float>();
        float max_gaussian = -1f;
        float min_gaussian = float.MaxValue;

        for (int i = 0; i < vertices.Length; i++)
        {
            float dx = vertices[i].x - centre_point.x;
            float dy = vertices[i].y - centre_point.y;
            float dz = vertices[i].z - centre_point.z;
            float distance = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
            //print(distance);
            if (distance < affect_region && vertices[i].y > -0.06f)
            {
                float z_gaussian = Gaussian_2d(dx * 10, dy * 10);
                change_vertices.Add(i, z_gaussian);
                if (max_gaussian < z_gaussian)
                    max_gaussian = z_gaussian;
                if (min_gaussian > z_gaussian)
                    min_gaussian = z_gaussian;
            }
        }

        // calculate ratio and interpolate between 0 and max dz
        change_vertices = change_vertices.ToDictionary(x => x.Key, x => (x.Value - min_gaussian) / (max_gaussian - min_gaussian));
        bool head_flag = false;
        foreach (var vertex in change_vertices)
        {
            float push_delta_factor = Mathf.Lerp(0, max_dz, vertex.Value);
            //Vector3 norm = normals[vertex.Key];
            //Vector3 push_point_norm = belly.transform.TransformDirection(norm); // to world direction
            Vector3 push_point_dir = -transform.forward;
            Vector3 push_delta_change = new Vector3(push_point_dir.x * push_delta_factor, push_point_dir.y * push_delta_factor, push_point_dir.z * push_delta_factor);
            vertices[vertex.Key].x -= push_delta_change.x;
            vertices[vertex.Key].y -= push_delta_change.y;
            vertices[vertex.Key].z -= push_delta_change.z;
            //print("push: x = " + push_delta_change.x + "y = " + push_delta_change.y + "z = " + push_delta_change.z);
            Vector3 push_point = belly.transform.TransformPoint(vertices[vertex.Key]); // to world position
            Vector3 fetus_delta_change = CheckFetusPos(push_point, push_point_dir);
            fetus_delta_change = belly.transform.InverseTransformVector(fetus_delta_change);
            //print("refine: x = " + fetus_delta_change.x + "y = " + fetus_delta_change.y + "z = " + fetus_delta_change.z);
            

            
            // avoid moving above original pos
            if (fetus_delta_change.y - push_delta_change.y > 0 || 
                ((fetus_delta_change.z - push_delta_change.z > 0) && (fetus_delta_change.z > 0)) ||
                ((fetus_delta_change.z - push_delta_change.z < 0) && (fetus_delta_change.z < 0)))
            {
                vertices[vertex.Key].x = original_pos[vertex.Key].x;
                vertices[vertex.Key].y = original_pos[vertex.Key].y;
                vertices[vertex.Key].z = original_pos[vertex.Key].z;
            }
            else
            {
                vertices[vertex.Key].x += fetus_delta_change.x;
                vertices[vertex.Key].y += fetus_delta_change.y;
                vertices[vertex.Key].z += fetus_delta_change.z;
            }
            
            if (!head_flag)
            {
                if (fetus_delta_change.y != 0 )
                    head_flag = true;
            }
        }

        if (head_flag)
        {
            help_text.text = "Something is here";
            laser_line_renderer.sharedMaterial = fetus_head_area;
            if(!sound_effect.isPlaying)
                sound_effect.Play();
        }
        else
        {
            help_text.text = "Not touched anything";
            laser_line_renderer.sharedMaterial = surgeon_area;
            if (sound_effect.isPlaying)
                sound_effect.Stop();
        }

        // debug pos and norm

        Vector3 pos, dir;
        pos = belly.transform.TransformPoint(original_pos[modify_index]); // to world position
        dir = new Vector3(0f, -1f, 0f);
       
        fetus_line.SetPosition(0, pos); // debug position
        fetus_line.SetPosition(1, pos - 20 * dir);

        // update the belly mesh filter
        mesh_to_modify.vertices = vertices;
        mesh_to_modify.RecalculateNormals();

        /*
         * DEBUG POSITION PRECISION
         *
        Mesh mesh_to_modify = fetus.GetComponent<SkinnedMeshRenderer>().sharedMesh;
        Vector3 vertex_pos = mesh_to_modify.vertices[modify_index];
        Color[] colors = new Color[mesh_to_modify.vertices.Length];
        colors[modify_index] = new Color(1, 1, 1);
        mesh_to_modify.colors = colors;
        help_text.text = "cut at vertex " + modify_index + ", Local pos:"  + vertex_pos.x + ", " + vertex_pos.y + ", " + vertex_pos.z ;
        */
    }

    Vector3 CheckFetusPos(Vector3 pos, Vector3 norm)
    {
        //  Ray ray = new Ray(pos, new Vector3(0f, -1f, 0f)); // vertical dir
        Ray ray = new Ray(pos, -norm); // norm dir

        // reference: https://answers.unity.com/questions/282165/raycastall-returning-results-in-reverse-order-of-c-1.html
        RaycastHit[] hits = Physics.RaycastAll(ray, 15f).OrderBy(h => h.distance).ToArray();
        
        if (hits.Length > 0  && hits[0].collider.tag == "Head")
        {
            print(hits[0].distance);
            
            if (hits[0].distance < 10f)
            {
                float dis = (10f - hits[0].distance) * 0.2f;
                return new Vector3(dis * norm.x, dis * norm.y, dis * norm.z);
            }
        }

        if (hits.Length > 0 && hits[0].collider.tag == "Back")
        {
            print(hits[0].distance);

            if (hits[0].distance < 5f)
            {
                float dis = (5f - hits[0].distance) * 0.2f;
                return new Vector3(dis * norm.x, dis * norm.y, dis * norm.z);
            }
        }
        
        return Vector3.zero;
    }

    void ResetModel()
    {
        Mesh mesh_to_modify = belly.GetComponent<MeshFilter>().sharedMesh;
        mesh_to_modify.vertices = original_pos;
        mesh_to_modify.normals = original_normals;
        belly.GetComponent<MeshCollider>().sharedMesh = null;
        belly.GetComponent<MeshCollider>().sharedMesh = mesh_to_modify;
    }
}



