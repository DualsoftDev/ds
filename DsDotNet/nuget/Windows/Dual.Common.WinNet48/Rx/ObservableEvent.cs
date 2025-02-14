using System;
using System.Runtime.InteropServices;
using Dsu.Common.Interfaces;

namespace Dsu.Common.Utilities
{
    /// <summary>
    /// Rx: System.Reactive.Subjects.Subject 에 대한 base interface
    /// </summary>
    [ComVisible(false)]
    public interface IObservableEvent
    {
    }

    public interface IObservableUIEvent : IObservableEvent {}

    /// <summary>
    /// 문서 내에서 발생하는 observable event
    /// </summary>
    public interface IObservableDocumentEvent : IObservableEvent
    {
        /// <summary>
        /// Event 가 발생한 doucment 에 대한 hint.  Nullable.
        /// </summary>
        IDocument DocumentHint { get; }
    }

    /// <summary>
    /// Appliaction 내에서 발생하는 observable event
    /// </summary>
    public interface IObservableApplicationEvent : IObservableEvent { }

    public enum ApplicationEventType
    {
        Loaded, Closing,
    }

    public class ObservableApplicationEvent : IObservableApplicationEvent
    {
        public ApplicationEventType Type { get; private set; }
        public ObservableApplicationEvent(ApplicationEventType type) { Type = type; } 
    }

    public interface ISimulationEvent : IObservableEvent { }

    public class ObservableDocumentEvent : IObservableDocumentEvent, IReason
    {
        public string Reason { get; private set; }
        public ObservableDocumentEvent(IDocument doc=null, string reason = null)
        {
            Reason = reason ?? String.Empty;
            DocumentHint = doc;
        }

        private static ObservableDocumentEvent _default = new ObservableDocumentEvent(null, "DefaultDocumentChangeEvent");
        public static ObservableDocumentEvent Default { get { return _default; } }
        public IDocument DocumentHint { get; private set; }
    }




    public enum DocumentChangeType
    {
        Loaded, Closing,
    }
    public class EnumerableDocumentChangedEvent : IObservableDocumentEvent
    {
        public DocumentChangeType DocumentChangeType { get; private set; }
        public EnumerableDocumentChangedEvent(IDocument doc, DocumentChangeType type)
        {
            DocumentChangeType = type;
            DocumentHint = doc;
        }

        public IDocument DocumentHint { get; private set; }
    }


    /// <summary>
    /// 여러 document 를 관장하는 객체(Application) 에서
    /// 개별 document 에 대한 공지를 통합적으로 받기 위한 class
    /// </summary>
    public class IntraDocumentChangedEvent : IObservableDocumentEvent
    {
        public IDocument Document { get; private set; }
        public IDocument DocumentHint { get { return Document;} }
        public IObservableDocumentEvent InnerEvent { get; internal set; }
        public IntraDocumentChangedEvent(IDocument doc, IObservableDocumentEvent innerEvent)
        {
            Document = doc;
            InnerEvent = innerEvent;
        }
    }
}