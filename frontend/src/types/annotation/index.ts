// Annotation types
export type {
    Annotation,
    AnnotationCoordinates,
    AnnotationTypeValue,
    PointAnnotationData,
    LineAnnotationData,
    BoundingBoxAnnotationData,
    PolylineAnnotationData,
    PolygonAnnotationData,
    TextAnnotationData
} from '../workspace/annotation';

// Annotation DTOs
export type {
    AnnotationDto,
    CreateAnnotationDto,
    UpdateAnnotationDto,
} from './dto';

// Annotation request types
export type {
    AnnotationListParams,
    CreateAnnotationRequest,
    UpdateAnnotationRequest,
} from './requests';
