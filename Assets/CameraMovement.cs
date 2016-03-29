using UnityEngine;
using System.Collections;

public class CameraMovement : MonoBehaviour {
    public GameObject PlayerBody;
    public float walkSpeed = 1f;
    private bool isRunning;
    public float runSpeedMultiplier = 3f;
    public float strafeDivider = 4;
    public Vector3 ForceBulletHit = new Vector3(0, 1f, 100f);
    public GameObject GunModel;
	// Use this for initialization
	void Start () {
        x = transform.rotation.x;
    }

    // Update is called once per frame
    void Update ()
    {
        if (Input.GetKey(KeyCode.Escape))
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);

        //if(PlayerBody.transform.position.y <= 1)
        //{
        //    var groundLevel = PlayerBody.transform.position;
        //    groundLevel.y = 1;
        //    PlayerBody.transform.position = groundLevel;
        //}

        if (Input.GetKey(KeyCode.LeftShift) && !isRunning || !Input.GetKey(KeyCode.LeftShift) && isRunning)
            toggleRun();

        if (Input.GetKey(KeyCode.W))
            PlayerBody.transform.Translate(Vector3.forward * GetMovementSpeed(false));
        if (Input.GetKey(KeyCode.A))
            PlayerBody.transform.Translate(Vector3.left * GetMovementSpeed(true));
        if (Input.GetKey(KeyCode.S))
            PlayerBody.transform.Translate(Vector3.back * GetMovementSpeed(false));
        if (Input.GetKey(KeyCode.D))
            PlayerBody.transform.Translate(Vector3.right* GetMovementSpeed(true));
            

        if (Input.GetMouseButton(0))
            Fire();

        RotateCam();
    }

    private float xSpeed = 180.0f;
    private float x = 0.0f;

    private void RotateCam()
    {
        x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;

        var rotation = Quaternion.Euler(0f, x, 0f);

        transform.rotation = rotation;
    }
    
    private void Fire()
    {
        // Bullet for visual effect
        //var bullet = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        //bullet.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        //bullet.transform.position = PlayerBody.transform.position + (PlayerBody.transform.forward * 2f);
        //var bulletBody = bullet.AddComponent<Rigidbody>();
        //bulletBody.useGravity = false;
        //bulletBody.AddForceAtPosition(PlayerBody.transform.forward * 50f, PlayerBody.transform.position, ForceMode.Impulse);
        //bulletBody.isKinematic = true;
        //bulletBody.mass = 1;
        //bullet.GetComponent<Renderer>().material.color = Color.red;

        var ray = new Ray(PlayerBody.transform.position, PlayerBody.transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000f))
        {
            hit.collider.GetComponent<Rigidbody>().AddForce(ForceBulletHit);
        }
    }


    public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
    public RotationAxes axes = RotationAxes.MouseXAndY;
    public float sensitivityX = 15F;
    public float sensitivityY = 15F;

    public float minimumY = -60F;
    public float maximumY = 60F;

    float rotationY = 0F;

    private void RotateGun()
    {
        if (GunModel.Equals(null)) return;

        if (axes == RotationAxes.MouseXAndY)
        {
            float rotationX = -180 + transform.localEulerAngles.y + Input.GetAxis("Mouse X") * sensitivityX;

            rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
            rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

            GunModel.transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
        }
        else if (axes == RotationAxes.MouseX)
        {
            GunModel.transform.Rotate(0, Input.GetAxis("Mouse X") * sensitivityX, 0);
        }
        else
        {
            rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
            rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);

            GunModel.transform.localEulerAngles = new Vector3(-rotationY, transform.localEulerAngles.y, 0);
        }
    }


    private float GetMovementSpeed(bool isStrafing)
    {
        var currentSpeed = walkSpeed;

        if (isRunning)
            currentSpeed *= runSpeedMultiplier;
        if(isStrafing)
            currentSpeed /= strafeDivider;

        return currentSpeed;
    }

    private void toggleRun()
    {
        isRunning = !isRunning;
    }
}
