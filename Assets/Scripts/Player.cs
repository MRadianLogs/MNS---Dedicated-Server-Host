using UnityEngine;

public class Player : MonoBehaviour
{
    public int id;
    public string username;

    private float moveSpeed = 5f / Constants.TICKS_PER_SEC;
    private bool[] movementInputs;

    public void Initialize(int newId, string newUsername)
    {
        id = newId;
        username = newUsername;

        movementInputs = new bool[4];
    }

    public void FixedUpdate()
    {
        Vector2 inputDirection = Vector2.zero;
        if (movementInputs[0])
        {
            inputDirection.y += 1; //W
        }
        if (movementInputs[1])
        {
            inputDirection.x -= 1; //A  //T:S
        }
        if (movementInputs[2])
        {
            inputDirection.y -= 1; //S //T:A
        }
        if (movementInputs[3])
        {
            inputDirection.x += 1; //D
        }

        Move(inputDirection);
    }

    private void Move(Vector2 inputDirection)
    {
        Vector3 moveDirection = transform.right * inputDirection.x + transform.forward * inputDirection.y;
        transform.position += moveDirection * moveSpeed;

        ServerSend.PlayerPosition(this);
        ServerSend.PlayerRotation(this);
    }

    public void SetInput(bool[] inputs, Quaternion newRotation)
    {
        movementInputs = inputs;
        transform.rotation = newRotation;
    }
}
