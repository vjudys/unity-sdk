SpeechToText service was originally implemented as a part of Watson SDK for Unity.
After Willow project rearchitecture, it was deemed necessary to channel STT data through WebSocket connection to XRayBackend.
The modifications in this class reflect the fact that all REST calls are eliminated as well as method associated with them.
Defines ENABLE_GET_MODEL_FUNCTION and ENABLE_RECOGNIZE_FUNCTION were used to remove portion of related code through our the project to ensure that the REST calls associated with those methods are never called.