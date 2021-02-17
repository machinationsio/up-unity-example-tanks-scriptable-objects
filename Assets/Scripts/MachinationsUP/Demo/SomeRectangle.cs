using System;
using UnityEngine;
using UnityEngine.UI;

public class SomeRectangle : MonoBehaviour
{

    public SomeScriptableObject ScriptableProperties;

    public bool horizontal = true;

    public Button ForceRefresh;
    public Text StatusText;

    Rigidbody2D rigidbody2d;
    float remainingTimeToChange;
    Vector2 direction = Vector2.right;

    private bool initializedScale = false;

    // Start is called before the first frame update
    void Start ()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
        remainingTimeToChange = (float) ScriptableProperties.ChangeDirectionTime.CurrentValue / 25;

        ScriptableProperties.OnUpdatedFromMachinations += ScriptablePropertiesOnOnUpdatedFromMachinations;

        direction = horizontal ? Vector2.right : Vector2.down;

        Button btn = ForceRefresh.GetComponent<Button>();
        btn.onClick.AddListener(TaskOnClick);
    }

    private void ScriptablePropertiesOnOnUpdatedFromMachinations (object sender, EventArgs e)
    {
        SomeScriptableObject obj = (SomeScriptableObject) sender;
        try
        {
            gameObject.transform.localScale = new Vector3((float) obj.SizeX.CurrentValue / 100,
                (float) obj.SizeY.CurrentValue / 100, (float) obj.SizeZ.CurrentValue / 100);
        }
        catch (Exception ex)
        {
            
            Debug.LogException(ex);
        }
    }

    void TaskOnClick ()
    {
        Debug.Log("Force Refresh coming soon!");
        StatusText.text += "\r\nYou forced a refresh";
    }

    // Update is called once per frame
    void Update ()
    {
        remainingTimeToChange -= Time.deltaTime;

        if (!initializedScale)
        {
            initializedScale = true;
            gameObject.transform.localScale = new Vector3((float) ScriptableProperties.SizeX.CurrentValue / 100,
                (float) ScriptableProperties.SizeY.CurrentValue / 100, (float) ScriptableProperties.SizeZ.CurrentValue / 100);
        }

        if (remainingTimeToChange <= 0)
        {
            remainingTimeToChange += (float) ScriptableProperties.ChangeDirectionTime.CurrentValue / 25;
            direction *= -1;
        }

        rigidbody2d.MovePosition(rigidbody2d.position + direction * (ScriptableProperties.MovementSpeed.CurrentValue * Time.deltaTime));
    }

}