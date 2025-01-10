using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SubnauticaShadows
{
    public class Shadow : MonoBehaviour
    {
        public string id;

        public Vector3 targetPosition = Vector3.zero;

        public Quaternion targetRotation = Quaternion.identity;

        public void Update()
        {
            transform.position = Vector3.Slerp(transform.position, targetPosition, Time.deltaTime * 4f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 4f);
        }
    }
}
