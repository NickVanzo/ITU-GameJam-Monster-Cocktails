using System.Collections;

// A floor the Elevator can take the player to. Each level scene needs one component implementing this.
public interface ILevel
{
    // Runs the whole visit, from arriving with the elevator until the level is done and ready to unload.
    IEnumerator Play(LevelLoader elevator);

    // Ends the visit early, e.g. when the player takes the elevator back up.
    void Leave();
}
