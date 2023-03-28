#include <stddef.h>
#include <cstdlib>
#include <map>
#include <string>


#ifdef EMSCRIPTEN
    #include <GLES2/gl2.h>
    //#include <GLES2/gl2ext.h>
    #include <emscripten.h>

    #include "zappar.h"

    #if defined(__CYGWIN32__)
        #define UNITY_INTERFACE_API __stdcall
        #define UNITY_INTERFACE_EXPORT __declspec(dllexport)
    #elif defined(WIN32) || defined(_WIN32) || defined(__WIN32__) || defined(_WIN64) || defined(WINAPI_FAMILY)
        #define UNITY_INTERFACE_API __stdcall
        #define UNITY_INTERFACE_EXPORT __declspec(dllexport)
    #elif defined(__MACH__) || defined(__ANDROID__) || defined(__linux__)
        #define UNITY_INTERFACE_API
        #define UNITY_INTERFACE_EXPORT
    #else
        #define UNITY_INTERFACE_API
        #define UNITY_INTERFACE_EXPORT
    #endif

	//PFNGLMAPBUFFEROESPROC glMapBufferOES = nullptr;
	//PFNGLUNMAPBUFFEROESPROC glUnmapBufferOES = nullptr;

    typedef void (UNITY_INTERFACE_API * UnityRenderingEvent)(int eventId);

    // ------------ Camera Frame Process ------------ // 

    extern "C" UnityRenderingEvent UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API zappar_process_callback_gl();
    static void UNITY_INTERFACE_API OnWebGLRenderEvent(int eventID);

    EM_JS(void, zappar_issue_js_plugin_render_event, (), {
        window.zappar_native_callbacks.process_gl();
    });

    static void UNITY_INTERFACE_API OnWebGLRenderEvent(int eventID)
    {
        zappar_issue_js_plugin_render_event();
    }

    extern "C" UnityRenderingEvent UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API zappar_process_callback_gl()
    {
        return OnWebGLRenderEvent;
    }

    // ------------ Camera Frame Upload ------------ // 

    extern "C" UnityRenderingEvent UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API zappar_upload_callback_native_gl();
    static void UNITY_INTERFACE_API OnWebGLUploadEvent(int eventID);

    EM_JS(void, zappar_issue_js_plugin_upload_gl_event, (), {
        window.zappar_native_callbacks.upload_gl();
    });

    static void UNITY_INTERFACE_API OnWebGLUploadEvent(int eventID)
    {
        zappar_issue_js_plugin_upload_gl_event();
    }

    extern "C" UnityRenderingEvent UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API zappar_upload_callback_native_gl()
    {
        return OnWebGLUploadEvent;
    }
    
    // ------------ Face Mesh Buffer Native Update ------------ // 

    static std::map<zappar_face_mesh_t,std::pair<void*,int>> s_faceMeshVertexBuffers; //vertex buffer handle and count for each face mesh pipeline
    // Used here for updating Unity face mesh vertex buffer natively
    struct MeshVertex
    {
    	float pos[3];
    	float normal[3];
    	//float color[4];
    	float uv[2];
    };

    EM_JS(void, log_string, (const char *msg), {
        console.log(UTF8ToString(msg));
    });

    extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API zappar_set_unity_face_mesh_buffer(zappar_face_mesh_t faceMesh, void* vertexBufferHandle, int vertexCount)
    {
        if(vertexBufferHandle==nullptr || vertexCount<=0){
            log_string("Invalid vertex buffer handle or vertexCount, in zappar_set_mesh_buffers_from_unity in face mesh pipeline!");
            return;
        }
        s_faceMeshVertexBuffers[faceMesh] = std::make_pair(vertexBufferHandle,vertexCount);
        //log_string(("Saved face mesh for update: " + std::to_string((int)faceMesh)).c_str());
    }

    extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API zappar_clear_unity_face_mesh_buffer(zappar_face_mesh_t faceMesh)
    {
        auto it = s_faceMeshVertexBuffers.find(faceMesh);
        if(it==s_faceMeshVertexBuffers.end()) return;
        s_faceMeshVertexBuffers.erase(it);
        //log_string(("Cleared face mesh for update: " + std::to_string((int)faceMesh)).c_str());
    }

    extern "C" int UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API zappar_get_unity_face_mesh_buffers_count(){
        return s_faceMeshVertexBuffers.size();
    }

    EM_JS(const float*, zappar_issue_js_plugin_face_mesh_vertices, (void* o), {
        return window.zappar_native_callbacks.face_mesh_vertices(o);
    });
    EM_JS(int, zappar_issue_js_plugin_face_mesh_vertices_size, (void* o), {
        return window.zappar_native_callbacks.face_mesh_vertices_size(o);
    });
    EM_JS(const float*, zappar_issue_js_plugin_face_mesh_normals, (void* o), {
        return window.zappar_native_callbacks.face_mesh_normals(o);
    });
    EM_JS(int, zappar_issue_js_plugin_face_mesh_normals_size, (void* o), {
        return window.zappar_native_callbacks.face_mesh_normals_size(o);
    });
    EM_JS(const float*, zappar_issue_js_plugin_face_mesh_uvs, (void* o), {
        return window.zappar_native_callbacks.face_mesh_uvs(o);
    });
    EM_JS(int, zappar_issue_js_plugin_face_mesh_uvs_size, (void* o), {
        return window.zappar_native_callbacks.face_mesh_uvs_size(o);
    });

    static void UNITY_INTERFACE_API OnNativeGLFaceMeshEvent(int eventID){
        if(eventID!=1011) { log_string("Invalid event id"); return;}
        
        // if(glMapBufferOES==nullptr)
        // glMapBufferOES = (PFNGLMAPBUFFEROESPROC)eglGetProcAddress("glMapBufferOES");
        // if(glUnmapBufferOES==nullptr)
		// glUnmapBufferOES = (PFNGLUNMAPBUFFEROESPROC)eglGetProcAddress("glUnmapBufferOES");

        for(auto& fmb : s_faceMeshVertexBuffers) { 
            int vertexCount = fmb.second.second;
            //int vCount = zappar_face_mesh_vertices_size(fmb.first);
            //Debug::Log("GL ",vertexCount," ",vCount);
            //assert(vCount==3*vertexCount);

            void* unityBufferHandle = fmb.second.first;
            if(!unityBufferHandle) continue;
            glBindBuffer(GL_ARRAY_BUFFER,(GLuint)(size_t)unityBufferHandle);
            GLint bufferSize = 0;
            glGetBufferParameteriv(GL_ARRAY_BUFFER, GL_BUFFER_SIZE, &bufferSize);
            
            //void* mapped = glMapBuffer(GL_ARRAY_BUFFER,GL_WRITE_ONLY); //Not supported on GLES2
            //void* mapped = glMapBufferOES(GL_ARRAY_BUFFER, GL_WRITE_ONLY_OES);
            void* mapped = malloc(bufferSize);

            //Modify vertex buffer
            if(!mapped) { log_string("failed to malloc buffer memory!");continue;}

            int vertexStride = int(bufferSize / vertexCount);
            if(sizeof(MeshVertex)*vertexCount!=bufferSize) 
                log_string(("vertex buffer size mismatch! BufferSize: "+std::to_string(bufferSize)+
                " VerticesCount: "+std::to_string(vertexCount)+" MeshVertexSize: "+std::to_string(sizeof(MeshVertex))).c_str());
            char* bufferPtr = (char*)mapped;
            //copy vertices and normals of face_mesh from zcv to unity_mesh
            const float* zFaceVerts = zappar_issue_js_plugin_face_mesh_vertices(fmb.first);
            const float* zFaceNorms = zappar_issue_js_plugin_face_mesh_normals(fmb.first);
            const float* zFaceUVs = zappar_issue_js_plugin_face_mesh_uvs(fmb.first);
            for(int i=0;i<vertexCount;++i){
                MeshVertex& unityVert = *(MeshVertex*)bufferPtr;
                unityVert.pos[0] = zFaceVerts[3*i+0];
                unityVert.pos[1] = zFaceVerts[3*i+1];
                unityVert.pos[2] = -zFaceVerts[3*i+2];
                unityVert.normal[0] = zFaceNorms[3*i+0];
                unityVert.normal[1] = zFaceNorms[3*i+1];
                unityVert.normal[2] = -zFaceNorms[3*i+2];
                unityVert.uv[0] = zFaceUVs[2*i+0];
                unityVert.uv[1] = zFaceUVs[2*i+1];
                bufferPtr += vertexStride;
            }
            //log_string("updating vbo SubData");
            //bufferPtr -= bufferSize; //vertexCount * vertexStride
            glBufferSubData(GL_ARRAY_BUFFER,0,bufferSize,mapped);
            //delete bufferPtr;
            free(mapped);
            // glBufferSubData(GL_ARRAY_BUFFER, 0, sizeof(float)*zFaceVertsSize, zFaceVerts);
            // glBufferSubData(GL_ARRAY_BUFFER, sizeof(float)*zFaceVertsSize, sizeof(float)*zFaceNormsSize, zFaceNorms);
            // glBufferSubData(GL_ARRAY_BUFFER, sizeof(float)*(zFaceVertsSize + zFaceNormsSize), sizeof(float)*zFaceUVsSize, zFaceUVs);
            // glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 3 * sizeof(float), 0);  
            // glVertexAttribPointer(1, 3, GL_FLOAT, GL_FALSE, 3 * sizeof(float), (void*)(sizeof(float)*zFaceVertsSize));  
            // glVertexAttribPointer(
            //   2, 2, GL_FLOAT, GL_FALSE, 2 * sizeof(float), (void*)(sizeof(float)*(zFaceVertsSize + zFaceNormsSize))); 

            //Close vertex buffer modification
            //glBindBuffer(GL_ARRAY_BUFFER,(GLuint)(size_t)unityBufferHandle);
            //glUnmapBuffer(GL_ARRAY_BUFFER);
            //glUnmapBufferOES(GL_ARRAY_BUFFER);
        }
    }
    extern "C" UnityRenderingEvent UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API zappar_update_face_mesh_buffer_callback_native_gl()
    {
        return OnNativeGLFaceMeshEvent;
    }

#endif