namespace MidiTweeter
{
    using System.Collections.Generic;

    public class TimeSlice
    {
        public int Start { get; set; }

        public int Stop { get; set; }

        public List<Note> NoteList { get; set; }

        public void AddNoteList(List<Note> noteList)
        {
            foreach (var note in noteList)
                NoteList.Add(note);
        }

        public TimeSlice(int start, int stop)
        {
            NoteList = new List<Note>();
            Start = start;
            Stop = stop;
        }

        public TimeSlice(int start, int stop, Note note)
        {
            NoteList = new List<Note>();
            Start = start;
            Stop = stop;
            NoteList.Add(note);
        }

        public override string ToString()
        {
            return string.Format("Start = {0} ; Stop = {1} ; NoteCount = {2}", Start, Stop, NoteList.Count);
        }
    }
}
