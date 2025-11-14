using UnityEngine;

public class lightsScript : MonoBehaviour
{
    [SerializeField] private float Speed = 10;// Œõ‚ÌÁ‚¦‚é‘¬“x
    [SerializeField] private int num = 0;// ‘Î‰‚·‚éƒL[
    private Renderer rend;
    private float alfa = 0;
    // Start is called before the first frame update
    void Start()
    {
        rend = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!(rend.material.color.a <= 0))
        {
            rend.material.color = new Color(rend.material.color.r, rend.material.color.r, rend.material.color.r, alfa);
        }

        if (num == 2)
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                colorChange();
            }
        }
        if (num == 3)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                colorChange();
            }
        }
        if (num == 4)
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
                colorChange();
            }
        }
        if (num == 5)
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                colorChange();
            }
        }
        alfa -= Speed * Time.deltaTime;

    }

    void colorChange()
    {
        alfa = 0.3f;
        rend.material.color = new Color(rend.material.color.r, rend.material.color.g, rend.material.color.b, alfa);
    }
}