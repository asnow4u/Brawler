using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAttack
{
    public void Setup(SceneObject obj);

    public void PerformUpAttack();
 
    public void PerformDownAttack();
    
    public void PerformRightAttack();
    
    public void PerformLeftAttack();
    

}
