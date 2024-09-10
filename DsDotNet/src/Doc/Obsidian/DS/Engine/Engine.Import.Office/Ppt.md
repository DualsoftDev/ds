- PptDoc
	- BuildSystem
- PptPage
- PptNode

#### Office PPT
- PresentationDocument  <--> PptDoc
- SlidePart  (PresentationDocument.PresentationPart.SlideParts)
	- Slide <--> PptPage
- GroupShapes
	- Slide.GetShapeTreeGroupShapes()
- Shape <--> PptNode ?
	- SlidePart.Slide.CommonSlideData.ShapeTree.Descendants<'TShape>
		- 'TShape: 
			- Presentation.Shape
			- Presentation.ConnectionShape
			- Presentation.GroupShape
	- SlidePart.SlideLayoutPart.CommonSlideData.ShapeTree.Descendants<'TShape>()



- Call Graph
     {
          loadSystem > BuildSystem > MakeSegment > createCallVertex > createCall > addNewCall,
                                   > MakeSegment > createAutoPre
     }
      > handleActionJob > getLibraryPathsAndParams > getNewDevice
      
      