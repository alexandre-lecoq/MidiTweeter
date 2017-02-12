namespace MidiTweeter
{
    public class Note
    {
        int _noteNumber;
        int _velocity;

        public int NoteNumber
        {
            get { return _noteNumber; }
            set { _noteNumber = value; }
        }

        public int Velocity
        {
            get { return _velocity; }
            set { _velocity = value; }
        }

        public Note(int noteNumber, int velocity)
        {
            _noteNumber = noteNumber;
            _velocity = velocity;
        }

        public override string ToString()
        {
            return "NoteNumber = " + _noteNumber + " ; Velocity = " + _velocity;
        }
    }
}
