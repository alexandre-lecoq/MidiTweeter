namespace MidiTweeter
{
    using System.Collections.Generic;

    public class TimeSlice
    {
        public int Start { get; }

        public int Stop { get; }

        public List<Note> NoteList { get; }

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
            return $"Start = {Start} ; Stop = {Stop} ; NoteCount = {NoteList.Count}";
        }
    }
}
