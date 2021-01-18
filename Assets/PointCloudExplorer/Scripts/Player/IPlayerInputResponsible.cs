using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerInputResponsible
{

    void OnKeyDown(KeyCode keycode);
    void OnKeyUp(KeyCode keycode);

}
