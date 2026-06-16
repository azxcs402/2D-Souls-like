using UnityEngine;

public class EnemyState : IState
{
    protected StateMachine stateMachine;
    protected Enemy enemy;
    protected Animator anim;

    protected Rigidbody2D rb;
    protected float stateTimer;
    protected bool triggerCalled;
    protected string animBoolName;

    public EnemyState(Enemy enemy, StateMachine stateMachine)
        : this(enemy, stateMachine, null)
    {
    }

    public EnemyState(Enemy enemy, StateMachine stateMachine, string animBoolName)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
        this.animBoolName = animBoolName;

        rb = enemy.rb;
        anim = enemy.anim;
    }

    public virtual void Enter()
    {
        triggerCalled = false;

        if (anim != null && !string.IsNullOrWhiteSpace(animBoolName))
        {
            anim.SetBool(animBoolName, true);
        }
    }

    public virtual void Update()
    {
        stateTimer -= Time.deltaTime;
        UpdateAnimationParameters();
    }

    public virtual void FixedUpdate()
    {

    }

    public virtual void Exit()
    {
        if (anim != null && !string.IsNullOrWhiteSpace(animBoolName))
        {
            anim.SetBool(animBoolName, false);
        }
    }

    public void AnimationTrigger()
    {
        triggerCalled = true;
    }

    public virtual void UpdateAnimationParameters()
    {
    }
}
