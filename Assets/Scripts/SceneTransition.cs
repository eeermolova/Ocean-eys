using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{

    public void Transition()
    {
        SceneManager.LoadScene(1);
      }
}