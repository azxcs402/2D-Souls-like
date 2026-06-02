using UnityEngine;

public class EnemyState : IState
{
    protected StateMachine stateMachine;
    protected Enemy enemy;

    protected Rigidbody2D rb;
    protected float stateTimer;

    public EnemyState(Enemy enemy, StateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;

        rb = enemy.rb;
    }

    public virtual void Enter()
    {

    }

    public virtual void Update()
    {
        stateTimer -= Time.deltaTime;
    }

    public virtual void FixedUpdate()
    {

    }

    public virtual void Exit()
    {

    }
}
