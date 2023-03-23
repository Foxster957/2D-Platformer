using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackingPointController : MonoBehaviour
{
    public GameObject player;
    private Rigidbody2D playerBody => player.GetComponent<Rigidbody2D>();
    private Vector3 playerPos => player.transform.position;
	private ContactFilter2D level;
    
    void Start()
    {
		level.SetLayerMask(LayerMask.GetMask("Level"));
		level.SetNormalAngle(-95, -85);
		level.useOutsideNormalAngle = true;
    }

    void Update()
    {
        Vector3 pos = new Vector3(playerPos.x, transform.position.y, 0f);

		if(playerBody.IsTouching(level))
		{
			pos.y = playerPos.y<10 ? playerPos.y : playerPos.y-3;
		}
		/*else if(Mathf.Abs(transform.position.y - playerPos.y) > 5)
		{
			pos.y = playerPos.y + Mathf.Sign(playerPos.y)*12;
		}*/

		transform.position = pos;
    }
}
