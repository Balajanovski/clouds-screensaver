Imports OpenTK
Imports GlmNet
Imports OpenTK.Graphics.OpenGL4

Public Class Camera
    ' Camera position
    Private pos As Vector3
    Public Property Position() As Vector3
        Get
            Return pos
        End Get
        Set(value As Vector3)
            pos = value

            ' Reset view matrix based on new value of position
            view = Matrix4.LookAt(pos, lookAt, New Vector3(0.0, 1.0, 0.0))
        End Set
    End Property

    ' Where camera is looking
    Private lookAt As Vector3
    Public Property LookAtTarget() As Vector3
        Get
            Return lookAt
        End Get
        Set(value As Vector3)
            lookAt = value

            view = Matrix4.LookAt(pos, lookAt, New Vector3(0.0, 1.0, 0.0))
        End Set
    End Property

    ' View matrix
    Private view As Matrix4
    Public ReadOnly Property ViewMatrix() As Matrix4
        Get
            Return view
        End Get
    End Property

    ' Projection matrix
    Private projection As Matrix4
    Public ReadOnly Property ProjectionMatrix() As Matrix4
        Get
            Return projection
        End Get
    End Property

    ' Field of View
    Private fieldOfView As Single
    Public ReadOnly Property FOV() As Single
        Get
            Return fieldOfView
        End Get
    End Property

    Private Shared Function opentkVecToGLMVec(tk As Vector4) As vec4
        Return New vec4(tk.X, tk.Y, tk.Z, tk.W)
    End Function

    Private Shared Function opentkVecToGLMVec(tk As Vector3) As vec3
        Return New vec3(tk.X, tk.Y, tk.Z)
    End Function

    Private Shared Function opentkMatToGLMMat(tk As Matrix4) As mat4
        Return New mat4(opentkVecToGLMVec(tk.Row0),
                        opentkVecToGLMVec(tk.Row1),
                        opentkVecToGLMVec(tk.Row2),
                        opentkVecToGLMVec(tk.Row3))
    End Function

    ' Project world space coords into screen space
    Public Function Project(wordSpacePos As Vector3) As Vector2
        Dim obj As vec3 = opentkVecToGLMVec(wordSpacePos)
        Dim viewMat As mat4 = opentkMatToGLMMat(view)
        Dim projMat As mat4 = opentkMatToGLMMat(projection)

        Dim viewportArr(4) As Integer
        GL.GetInteger(GetPName.Viewport, viewportArr)
        Dim viewport As vec4 = New vec4(viewportArr(0), viewportArr(1), viewportArr(2), viewportArr(3))

        Dim screenCoords = glm.project(obj, viewMat, projMat, viewport)
        Return New Vector2(screenCoords.x, screenCoords.y)
    End Function

    Public Sub New(newPos As Vector3,
                   lookAtTarget As Vector3,
                   screenWidth As Single,
                   screenHeight As Single,
                   Optional newFov As Single = 60.0F)
        lookAt = lookAtTarget
        pos = newPos
        fieldOfView = newFov

        view = Matrix4.LookAt(pos, lookAt, New Vector3(0.0, 1.0, 0.0))
        projection = Matrix4.CreatePerspectiveFieldOfView(CType(Math.PI, Single) * (fieldOfView / 180.0),
                                                          screenWidth / screenHeight, 0.2, 256.0)
    End Sub
End Class
