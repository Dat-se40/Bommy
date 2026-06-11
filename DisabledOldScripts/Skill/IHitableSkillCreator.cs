using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHitableSkillCreator
{
    event Action onAllCompleted;
    void CreateOnDirection(Vector2 position, Vector2 direction);
}
