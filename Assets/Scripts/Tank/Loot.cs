using UnityEngine;

namespace Tank
{
    public class Loot : MonoBehaviour
    {

        void Awake ()
        {
            transform.Translate(Vector3.up * 3);
        }

        void Update ()
        {
            // Move the object forward along its z axis 1 unit/second.
            //transform.Translate(Vector3.forward * Time.deltaTime);
            // Move the object upward in world space 1 unit/second.
            //transform.Translate(Vector3.up * Time.deltaTime, Space.World);
            
            //Rotate the loot nicely.
            transform.Rotate(0.1f, 0.1f, 0.1f, Space.Self);
        }

    }
}