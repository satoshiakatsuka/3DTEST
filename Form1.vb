Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Linq
Imports System.Windows.Forms

Public Class Form1
    Inherits Form

    Private animationTimer As Timer
    'コメント追加2026-01-17
    ' 重力定数（各重力源からの影響の大きさ）
    Private Const G As Single = 100000.0F

    ' 立方体の基本形状（中心原点、各辺100）
    Private cubeVertices As Vector3D()
    ' 各面の頂点インデックス
    Private faces As Integer()() = {
        New Integer() {0, 1, 2, 3},   ' 奥面
        New Integer() {4, 5, 6, 7},   ' 手前面
        New Integer() {0, 1, 5, 4},   ' 底面
        New Integer() {3, 2, 6, 7},   ' 天面
        New Integer() {0, 3, 7, 4},   ' 左面
        New Integer() {1, 2, 6, 5}    ' 右面
    }
    ' 各面の基本色
    Private faceBaseColors As Color() = {Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.Cyan, Color.Magenta}

    ' 複数の立方体を管理するリスト
    Private cubes As List(Of Cube)
    Private rand As New Random()

    ' 複数の重力源（初期は左・中央・右に配置）
    Private attractors As List(Of Vector3D)

    ' コンストラクタ
    Public Sub New()
        ' デザイナー生成コードの初期化（必ず最初に呼び出す）
        InitializeComponent()

        Me.DoubleBuffered = True
        Me.ClientSize = New Size(800, 600)
        Me.Text = "動く重力源によるスクリーンセーバー風マルチキューブ"

        ' タイマーの初期化（約60FPS）
        animationTimer = New Timer()
        animationTimer.Interval = 16
        AddHandler animationTimer.Tick, AddressOf AnimationTimer_Tick

        ' 立方体の基本形状（中心原点、各辺100）を初期化
        cubeVertices = New Vector3D() {
            New Vector3D(-50, -50, -50),
            New Vector3D(50, -50, -50),
            New Vector3D(50, 50, -50),
            New Vector3D(-50, 50, -50),
            New Vector3D(-50, -50, 50),
            New Vector3D(50, -50, 50),
            New Vector3D(50, 50, 50),
            New Vector3D(-50, 50, 50)
        }

        ' 重力源の初期化：初期位置は左・中央・右（後で動かします）
        attractors = New List(Of Vector3D) From {
            New Vector3D(-200, 0, 0),
            New Vector3D(0, 0, 0),
            New Vector3D(200, 0, 0)
        }

        ' 複数の立方体を初期化（例：10個）
        cubes = New List(Of Cube)
        For i As Integer = 1 To 10
            Dim cube As New Cube()

            ' 重力源からの距離をランダムに（例：150～300）
            Dim rVal As Single = CSng(rand.NextDouble() * 150 + 150)
            Dim theta As Single = CSng(rand.NextDouble() * 2 * Math.PI)
            Dim phi As Single = CSng(rand.NextDouble() * Math.PI)
            cube.Position = New Vector3D(rVal * Math.Sin(phi) * Math.Cos(theta),
                                         rVal * Math.Sin(phi) * Math.Sin(theta),
                                         rVal * Math.Cos(phi))
            ' 円軌道に必要な速度 v = √(G / r)（単一重力源を仮定しているが、近似として）
            Dim vMag As Single = CSng(Math.Sqrt(G / rVal))
            ' 立方体の位置（radial vector）に対して垂直な方向を求める
            Dim up As Vector3D = New Vector3D(0, 1, 0)
            If Math.Abs(Dot(cube.Position, up) / rVal) > 0.99 Then
                up = New Vector3D(1, 0, 0)
            End If
            Dim vDir As Vector3D = Normalize(Cross(cube.Position, up))
            cube.Velocity = vDir * vMag

            ' 初期回転角をランダムに設定
            cube.RotationX = CSng(rand.NextDouble() * 2 * Math.PI)
            cube.RotationY = CSng(rand.NextDouble() * 2 * Math.PI)
            cube.RotationZ = CSng(rand.NextDouble() * 2 * Math.PI)
            ' 回転速度もランダムに（例：0.01～0.03 rad/tick）
            cube.RotationSpeedX = CSng(rand.NextDouble() * 0.02 + 0.01)
            cube.RotationSpeedY = CSng(rand.NextDouble() * 0.02 + 0.01)
            cube.RotationSpeedZ = CSng(rand.NextDouble() * 0.02 + 0.01)

            cubes.Add(cube)
        Next
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        animationTimer.Start()
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias

        ' 背景をダークなグラデーションで描画
        Dim rect As New Rectangle(0, 0, Me.ClientSize.Width, Me.ClientSize.Height)
        Using bgBrush As New LinearGradientBrush(rect, Color.Black, Color.DarkBlue, 45)
            g.FillRectangle(bgBrush, rect)
        End Using

        Dim centerX As Single = ClientSize.Width / 2.0F
        Dim centerY As Single = ClientSize.Height / 2.0F

        ' 各立方体の描画
        For Each cube In cubes
            Dim transformed(cubeVertices.Length - 1) As Vector3D
            Dim projected(cubeVertices.Length - 1) As PointF

            Dim cosX As Single = CSng(Math.Cos(cube.RotationX))
            Dim sinX As Single = CSng(Math.Sin(cube.RotationX))
            Dim cosY As Single = CSng(Math.Cos(cube.RotationY))
            Dim sinY As Single = CSng(Math.Sin(cube.RotationY))
            Dim cosZ As Single = CSng(Math.Cos(cube.RotationZ))
            Dim sinZ As Single = CSng(Math.Sin(cube.RotationZ))

            For i As Integer = 0 To cubeVertices.Length - 1
                Dim v As Vector3D = cubeVertices(i)
                Dim x As Single = v.X
                Dim y As Single = v.Y
                Dim z As Single = v.Z

                ' X軸回転
                Dim y1 As Single = y * cosX - z * sinX
                Dim z1 As Single = y * sinX + z * cosX
                Dim x1 As Single = x

                ' Y軸回転
                Dim z2 As Single = z1 * cosY - x1 * sinY
                Dim x2 As Single = z1 * sinY + x1 * cosY
                Dim y2 As Single = y1

                ' Z軸回転
                Dim x3 As Single = x2 * cosZ - y2 * sinZ
                Dim y3 As Single = x2 * sinZ + y2 * cosZ
                Dim z3 As Single = z2

                ' 立方体の物理位置を反映
                x3 += cube.Position.X
                y3 += cube.Position.Y
                z3 += cube.Position.Z

                transformed(i) = New Vector3D(x3, y3, z3)

                ' 透視投影（カメラ位置を z=400 と仮定）
                Dim distance As Single = 400.0F
                Dim scale As Single = distance / (distance - z3)
                Dim px As Single = x3 * scale
                Dim py As Single = y3 * scale
                projected(i) = New PointF(px + centerX, py + centerY)
            Next

            ' 各面ごとの平均Zおよびライティング計算
            Dim faceList As New List(Of FaceInfo)
            For faceIndex As Integer = 0 To faces.Length - 1
                Dim idxs As Integer() = faces(faceIndex)
                Dim avgZ As Single = 0.0F
                For Each idx In idxs
                    avgZ += transformed(idx).Z
                Next
                avgZ /= idxs.Length

                ' 面の法線計算（3頂点から）
                Dim v0 As Vector3D = transformed(idxs(0))
                Dim v1 As Vector3D = transformed(idxs(1))
                Dim v2 As Vector3D = transformed(idxs(2))
                Dim u As Vector3D = New Vector3D(v1.X - v0.X, v1.Y - v0.Y, v1.Z - v0.Z)
                Dim vVec As Vector3D = New Vector3D(v2.X - v0.X, v2.Y - v0.Y, v2.Z - v0.Z)
                Dim normal As Vector3D = Normalize(Cross(u, vVec))

                ' 簡易ライティング：カメラ方向 (-Z) との内積で明るさ決定
                Dim dp As Single = Math.Max(0.0F, Dot(normal, New Vector3D(0, 0, -1)))
                Dim brightness As Single = 0.3F + 0.7F * dp

                Dim baseColor As Color = faceBaseColors(faceIndex Mod faceBaseColors.Length)
                Dim faceColor As Color = AdjustColorBrightness(baseColor, brightness)

                faceList.Add(New FaceInfo With {
                    .Indices = idxs,
                    .AvgZ = avgZ,
                    .Color = faceColor
                })
            Next

            ' 面を奥から手前に描画するため平均Zでソート
            faceList.Sort(Function(a, b) a.AvgZ.CompareTo(b.AvgZ))
            For Each face In faceList
                Dim pts As PointF() = face.Indices.Select(Function(idx) projected(idx)).ToArray()
                Using brush As New SolidBrush(face.Color)
                    g.FillPolygon(brush, pts)
                End Using
                g.DrawPolygon(Pens.Black, pts)
            Next
        Next

        ' 各重力源（attractors）の描画（小さな円）
        For Each attractor In attractors
            Dim distance As Single = 400.0F
            Dim scale As Single = distance / (distance - attractor.Z)
            Dim ax As Single = attractor.X * scale + centerX
            Dim ay As Single = attractor.Y * scale + centerY
            g.FillEllipse(Brushes.White, ax - 5, ay - 5, 10, 10)
        Next
    End Sub

    ' タイマーイベント：各立方体の物理状態・回転状態の更新および重力源の更新
    Private Sub AnimationTimer_Tick(sender As Object, e As EventArgs)
        Dim dt As Single = 0.016F ' 約16ms

        ' 重力源の位置を動的に更新
        UpdateAttractors()

        ' 各立方体の更新
        For Each cube In cubes
            ' 回転更新
            cube.RotationX += cube.RotationSpeedX
            cube.RotationY += cube.RotationSpeedY
            cube.RotationZ += cube.RotationSpeedZ

            ' 複数の重力源からの加速度を合算
            Dim totalAcceleration As Vector3D = New Vector3D(0, 0, 0)
            For Each attractor In attractors
                Dim delta As Vector3D = attractor - cube.Position
                Dim r As Single = CSng(Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z))
                If r > 0 Then
                    totalAcceleration += delta * (G / (r * r * r))
                End If
            Next

            cube.Velocity = cube.Velocity + totalAcceleration * dt
            cube.Position = cube.Position + cube.Velocity * dt
        Next

        Invalidate()
    End Sub

    ' 重力源の位置を動的に更新するメソッド
    Private Sub UpdateAttractors()
        ' 経過時間（秒単位）
        Dim t As Single = Environment.TickCount / 1000.0F

        ' 各重力源の位置を、三角関数を用いて動かす例
        If attractors.Count >= 3 Then
            ' 1つ目の重力源：X 固定、Y と Z を振動
            attractors(0) = New Vector3D(-200, 50 * CSng(Math.Sin(t * 1.5)), 50 * CSng(Math.Cos(t * 1.2)))
            ' 2つ目の重力源：Y 固定、X と Z を振動
            attractors(1) = New Vector3D(100 * CSng(Math.Cos(t * 1.3)), 0, 100 * CSng(Math.Sin(t * 1.1)))
            ' 3つ目の重力源：Z 固定、X と Y を振動
            attractors(2) = New Vector3D(200 * CSng(Math.Sin(t * 1.4)), 100 * CSng(Math.Cos(t * 1.2)), 0)
        End If
    End Sub

#Region "3Dベクトル補助処理"

    ' 3次元ベクトルの構造体（演算子オーバーロード付き）
    Private Structure Vector3D
        Public X As Single
        Public Y As Single
        Public Z As Single

        Public Sub New(x As Single, y As Single, z As Single)
            Me.X = x
            Me.Y = y
            Me.Z = z
        End Sub

        Public Shared Operator +(a As Vector3D, b As Vector3D) As Vector3D
            Return New Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z)
        End Operator

        Public Shared Operator -(a As Vector3D, b As Vector3D) As Vector3D
            Return New Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z)
        End Operator

        Public Shared Operator *(v As Vector3D, scalar As Single) As Vector3D
            Return New Vector3D(v.X * scalar, v.Y * scalar, v.Z * scalar)
        End Operator

        Public Shared Operator *(scalar As Single, v As Vector3D) As Vector3D
            Return New Vector3D(v.X * scalar, v.Y * scalar, v.Z * scalar)
        End Operator
    End Structure

    ' 内積
    Private Shared Function Dot(a As Vector3D, b As Vector3D) As Single
        Return a.X * b.X + a.Y * b.Y + a.Z * b.Z
    End Function

    ' 外積
    Private Shared Function Cross(a As Vector3D, b As Vector3D) As Vector3D
        Return New Vector3D(a.Y * b.Z - a.Z * b.Y,
                            a.Z * b.X - a.X * b.Z,
                            a.X * b.Y - a.Y * b.X)
    End Function

    ' 正規化
    Private Shared Function Normalize(v As Vector3D) As Vector3D
        Dim len As Single = CSng(Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z))
        If len = 0 Then Return v
        Return New Vector3D(v.X / len, v.Y / len, v.Z / len)
    End Function

    ' 色の明度調整（brightness は 0～1 の範囲）
    Private Function AdjustColorBrightness(baseColor As Color, brightness As Single) As Color
        Dim r As Integer = Math.Min(255, CInt(baseColor.R * brightness))
        Dim g As Integer = Math.Min(255, CInt(baseColor.G * brightness))
        Dim b As Integer = Math.Min(255, CInt(baseColor.B * brightness))
        Return Color.FromArgb(r, g, b)
    End Function

    ' 面情報クラス（ペインターズアルゴリズム用）
    Private Class FaceInfo
        Public Property Indices As Integer()
        Public Property AvgZ As Single
        Public Property Color As Color
    End Class

    ' 立方体クラス：各立方体の物理状態・回転状態を保持
    Private Class Cube
        Public Property Position As Vector3D
        Public Property Velocity As Vector3D
        Public Property RotationX As Single
        Public Property RotationY As Single
        Public Property RotationZ As Single
        Public Property RotationSpeedX As Single
        Public Property RotationSpeedY As Single
        Public Property RotationSpeedZ As Single
    End Class

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        'ここへ来て　その3

    End Sub

#End Region

    ' （エントリーポイントはデザイナー生成コードにより設定済み）
End Class



