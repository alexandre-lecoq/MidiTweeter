namespace MidiTweeter
{
    public class Note
    {
        public int NoteNumber { get; }

        public int Velocity { get; }

        public Note(int noteNumber, int velocity)
        {
            NoteNumber = noteNumber;
            Velocity = velocity;
        }

        public override string ToString()
        {
            return $"NoteNumber = {NoteNumber} ; Velocity = {Velocity}";
        }
    }
}
