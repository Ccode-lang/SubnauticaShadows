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

        public void Update()
        {
            transform.position = Vector3.Slerp(transform.position, targetPosition, Time.deltaTime * 0.9f);
        }
    }
}
