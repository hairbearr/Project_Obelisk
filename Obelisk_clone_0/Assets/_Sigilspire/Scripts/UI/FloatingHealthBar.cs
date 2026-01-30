using UnityEngine;
using UnityEngine.UIElements;

public class FloatingHealthBar : MonoBehaviour
{
    private Slider slider;
    public Camera cam;
    public Transform target;
    public Vector3 offset = new Vector3(0, 1, 0);


    public void UpdateHealthBar(float currentValue, float maxValue)
    {
        slider.value = currentValue / maxValue;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = target.position + offset;
        transform.rotation = cam.transform.rotation;
    }
}
