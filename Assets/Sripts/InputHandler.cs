using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InputHandler : MonoBehaviour
{
    public bool InputAllowed = true;
    public UnityEvent OnRightButtonPressed;
    public UnityEvent OnLeftButtonPressed;

    public UnityEvent OnRightButtonReleased;
    public UnityEvent OnLeftButtonReleased;

    bool RightPressed = false;
    bool LeftPressed = false;
    bool ButtonPressed
    {
        get
        {
            return RightPressed || LeftPressed;
        }
    }	

	void Update ()
    {
        if (InputAllowed)
        {
	        if (Input.GetAxis("Horizontal") > 0)
            {
                if (!ButtonPressed)
                    RightButtonPressed();
            }
            else if (Input.GetAxis("Horizontal") < 0)
            {
                if (!ButtonPressed)
                    LeftButtonPressed();
            }
            else
            {
                if (RightPressed)
                    RightButtonReleased();
                else if (LeftPressed)
                    LeftButtonReleased();
            }
        }
	}

    public void RightButtonPressed()
    {
        RightPressed = true;
        OnRightButtonPressed.Invoke();
    }

    public void LeftButtonPressed()
    {
        LeftPressed = true;
        OnLeftButtonPressed.Invoke();
    }

    public void RightButtonReleased()
    {
        RightPressed = false;
        OnRightButtonReleased.Invoke();
    }

    public void LeftButtonReleased()
    {
        LeftPressed = false;
        OnLeftButtonReleased.Invoke();
    }
    
}
