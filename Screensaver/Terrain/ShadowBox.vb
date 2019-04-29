Imports OpenTK


' Ports ThinMatrix's Shadow Map Orthographic Projection Matrix Calculation Code
' Original available here: https://www.dropbox.com/sh/g9vnfiubdglojuh/AACpq1KDpdmB8ZInYxhsKj2Ma/shadows?dl=0&preview=ShadowBox.java&subfolder_nav_tracking=1
Public Class ShadowBox

    Private Shared OFFSET As Single = 10
    Private Shared UP As Vector4 = New Vector4(0, 1, 0, 0)
    Private Shared FORWARD As Vector4 = New Vector4(0, 0, 1, 0) ' Possible error source in that forward may not be correct. Check if not working!!!!!
    Private Shared SHADOW_DISTANCE As Single = 100

    Private minX, maxX As Single
    Private minY, maxY As Single
    Private minZ, maxZ As Single
    Private lightViewMatrix As Matrix4
    Private cam As Camera

    Private farHeight, farWidth, nearHeight, nearWidth As Single

    '
    ' Creates a New shadow box And calculates some initial values relating to
    ' the camera's view frustum, namely the width and height of the near plane
    ' And (possibly adjusted) far plane.
    '  
    ' lightViewMatrix:
    '            - basically the "view matrix" of the light. Can be used to
    '            transform a point from world space into "light" space (i.e.
    '            changes a point's coordinates from being in relation to the
    '            world's axis to being in terms of the light's local axis).
    ' camera:
    '             - the in-game camera.
    '
    Public Sub New(ByRef lightViewMat As Matrix4, camera As Camera)
        lightViewMatrix = lightViewMat
        cam = camera
        calculateWidthsAndHeights()
    End Sub

    '
    ' Updates the bounds of the shadow box based on the light direction And the
    ' camera's view frustum, to make sure that the box covers the smallest area
    ' possible while still ensuring that everything inside the camera's view
    ' (within a certain range) will cast shadows.
    '
    Public Sub update()
        Dim rotation As Matrix4 = calculateCameraRotationMatrix()
        Dim forwardVector As Vector3 = New Vector3(Vector4.Transform(rotation, FORWARD).Xyz)

        Dim toFar As Vector3 = New Vector3(forwardVector)
        toFar = Vector3.Multiply(toFar, SHADOW_DISTANCE)
        Dim toNear As Vector3 = New Vector3(forwardVector)
        toNear = Vector3.Multiply(toNear, Camera.NEAR_PLANE)
        Dim centerNear As Vector3 = Vector3.Add(toNear, cam.Position)
        Dim centerFar As Vector3 = Vector3.Add(toFar, cam.Position)

        Dim points As Vector4() = calculateFrustumVertices(rotation, forwardVector, centerNear, centerFar)

        Dim first As Boolean = True
        For Each point In points
            If first Then
                minX = point.X
                maxX = point.X
                minY = point.Y
                maxY = point.Y
                minZ = point.Z
                maxZ = point.Z
                first = False
                Continue For
            End If

            If (point.X > maxX) Then
                maxX = point.X
            ElseIf (point.X < minX) Then
                minX = point.X
            End If

            If (point.Y > maxY) Then
                maxY = point.Y
            ElseIf (point.Y < minY) Then
                minY = point.Y
            End If

            If (point.Z > maxZ) Then
                maxZ = point.Z
            ElseIf (point.Z < minZ) Then
                minZ = point.Z
            End If
        Next

        maxZ += OFFSET

    End Sub

    '
    ' Calculates the center of the "view cuboid" in light space first, And then
    ' converts this to world space using the inverse light's view matrix.
    ' 
    ' Returns the center of the "view cuboid" in world space.
    '
    Public Function getCenter() As Vector3
        Dim x As Single = (minX + maxX) / 2.0F
        Dim y As Single = (minY + maxY) / 2.0F
        Dim z As Single = (minZ + maxZ) / 2.0F
        Dim cen As Vector4 = New Vector4(x, y, z, 1)
        Dim invertedLight As Matrix4 = New Matrix4()
        invertedLight = Matrix4.Invert(lightViewMatrix)
        Return New Vector3(Vector4.Transform(invertedLight, cen).Xyz)
    End Function

    '
    ' Return The width of the "view cuboid" (orthographic projection area).
    '
    Public Function getWidth() As Single
        Return maxX - minX
    End Function

    '
    ' Return The height of the "view cuboid" (orthographic projection area).
    '
    Public Function getHeight() As Single
        Return maxY - minY
    End Function

    '
    ' Return The length of the "view cuboid" (orthographic projection area).
    '
    Public Function getLength() As Single
        Return maxZ - minZ
    End Function

    '
    ' Calculates the position of the vertex at each corner of the view frustum
    ' in light space (8 vertices in total, so this returns 8 positions).
    ' 
    ' Param rotation
    '            - camera's rotation.
    ' Param forwardVector
    '            - the direction that the camera Is aiming, And thus the
    '            direction of the frustum.
    ' Param centerNear
    '            - the center point of the frustum's near plane.
    ' Param centerFar
    '            - the center point of the frustum's (possibly adjusted) far
    '            plane.
    ' Return The positions of the vertices of the frustum in light space.
    '
    Private Function calculateFrustumVertices(rotation As Matrix4, forwardVector As Vector3,
            centerNear As Vector3, centerFar As Vector3) As Vector4()
        Dim upVector As Vector3 = New Vector3(Vector4.Transform(rotation, UP))
        Dim rightVector As Vector3 = Vector3.Cross(forwardVector, upVector)
        Dim downVector As Vector3 = New Vector3(-upVector.X, -upVector.Y, -upVector.Z)
        Dim leftVector As Vector3 = New Vector3(-rightVector.X, -rightVector.Y, -rightVector.Z)
        Dim farTop As Vector3 = Vector3.Add(centerFar, New Vector3(upVector.X * farHeight,
                upVector.Y * farHeight, upVector.Z * farHeight))
        Dim farBottom As Vector3 = Vector3.Add(centerFar, New Vector3(downVector.X * farHeight,
                downVector.Y * farHeight, downVector.Z * farHeight))
        Dim nearTop As Vector3 = Vector3.Add(centerNear, New Vector3(upVector.X * nearHeight,
                upVector.Y * nearHeight, upVector.Z * nearHeight))
        Dim nearBottom As Vector3 = Vector3.Add(centerNear, New Vector3(downVector.X * nearHeight,
                downVector.Y * nearHeight, downVector.Z * nearHeight))
        Dim points(7) As Vector4
        points(0) = calculateLightSpaceFrustumCorner(farTop, rightVector, farWidth)
        points(1) = calculateLightSpaceFrustumCorner(farTop, leftVector, farWidth)
        points(2) = calculateLightSpaceFrustumCorner(farBottom, rightVector, farWidth)
        points(3) = calculateLightSpaceFrustumCorner(farBottom, leftVector, farWidth)
        points(4) = calculateLightSpaceFrustumCorner(nearTop, rightVector, nearWidth)
        points(5) = calculateLightSpaceFrustumCorner(nearTop, leftVector, nearWidth)
        points(6) = calculateLightSpaceFrustumCorner(nearBottom, rightVector, nearWidth)
        points(7) = calculateLightSpaceFrustumCorner(nearBottom, leftVector, nearWidth)
        Return points
    End Function

    '
    ' Calculates one of the corner vertices of the view frustum in world space
    ' And converts it to light space.
    ' 
    ' Param startPoint
    '            - the starting center point on the view frustum.
    ' Param direction
    '            - the direction of the corner from the start point.
    ' Param width
    '            - the distance of the corner from the start point.
    ' Return - The relevant corner vertex of the view frustum in light space.
    '
    Private Function calculateLightSpaceFrustumCorner(startPoint As Vector3, direction As Vector3,
            width As Single) As Vector4
        Dim point As Vector3 = Vector3.Add(startPoint,
                New Vector3(direction.X * width, direction.Y * width, direction.Z * width))
        Dim point4f As Vector4 = New Vector4(point.X, point.Y, point.Z, 1.0F)
        point4f = Vector4.Transform(lightViewMatrix, point4f)
        Return point4f
    End Function

    '
    ' Return The rotation of the camera represented as a matrix.
    ' Calculates it based on: https://stackoverflow.com/questions/17325696/how-to-get-the-camera-rotation-matrix-out-of-the-model-view-matrix
    '
    Private Function calculateCameraRotationMatrix() As Matrix4
        Dim rotation As Matrix4 = cam.ViewMatrix

        rotation(0, 3) = rotation(1, 3) = rotation(2, 3) = rotation(3, 0) =
            rotation(3, 1) = rotation(3, 2) = 0.0
        rotation(3, 3) = 1.0

        rotation = Matrix4.Invert(rotation)

        Return rotation
    End Function

    '
    ' Calculates the width And height of the near And far planes of the
    ' camera's view frustum. However, this doesn't have to use the "actual" far
    ' plane of the view frustum. It can use a shortened view frustum if desired
    ' by bringing the far-plane closer, which would increase shadow resolution
    ' but means that distant objects wouldn't cast shadows.
    '
    Private Sub calculateWidthsAndHeights()
        farWidth = SHADOW_DISTANCE * Math.Tan(MathHelper.DegreesToRadians(cam.FOV))
        nearWidth = Camera.NEAR_PLANE _
                * Math.Tan(MathHelper.DegreesToRadians(cam.FOV))
        farHeight = farWidth / cam.AspectRatio
        nearHeight = nearWidth / cam.AspectRatio
    End Sub
End Class
