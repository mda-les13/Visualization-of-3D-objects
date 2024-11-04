using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HelixToolkit.Wpf;
using Microsoft.Win32;
using OxyPlot.Series;
using OxyPlot;
using OxyPlot.Wpf;
using System.Security.Policy;
using System.IO;
using System;

namespace PKG_Lab6
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Point3DCollection originalPoints; // Хранение оригинальных точек
        private Transform3DGroup _transformGroup; // Группа трансформаций
        private Matrix3D _currentTransformationMatrix; // Текущая матрица преобразования

        public MainWindow()
        {
            this.MinHeight = 1080;
            this.MinWidth = 1920;
            InitializeComponent();
            _transformGroup = new Transform3DGroup(); // Инициализация группы трансформаций
            _currentTransformationMatrix = Matrix3D.Identity; // Инициализация матрицы
        }

        private void LoadModel_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "OBJ files (*.obj)|*.obj"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                var reader = new ObjReader();
                var modelGroup = reader.Read(openFileDialog.FileName);
                var model = new ModelVisual3D { Content = modelGroup };
                helixViewport.Children.Clear(); // Очистка предыдущего содержимого
                helixViewport.Children.Add(model);
                // Извлечение точек для проекций
                originalPoints = ExtractPoints(modelGroup); // Сохраняем оригинальные точки для проекций
            }
        }

        private void Scale_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(TransformValueX.Text, out double scale) && scale >= 0.1 && scale <= 5.0)
            {
                var scaleTransform = new ScaleTransform3D(scale, scale, scale);
                _transformGroup.Children.Add(scaleTransform); // Добавляем к группе трансформаций
                UpdateCurrentTransformationMatrix(scaleTransform.Value); // Обновляем матрицу
                ApplyTransform(); // Применяем все трансформации
            }
            else
            {
                MessageBox.Show("Введите значение масштаба от 0.1 до 5.0", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Translate_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(TransformValueX.Text, out double translateX) &&
                double.TryParse(TransformValueY.Text, out double translateY) &&
                double.TryParse(TransformValueZ.Text, out double translateZ))
            {
                var translateTransform = new TranslateTransform3D(translateX, translateY, translateZ);
                _transformGroup.Children.Add(translateTransform); // Добавляем к группе трансформаций
                UpdateCurrentTransformationMatrix(translateTransform.Value); // Обновляем матрицу
                ApplyTransform(); // Применяем все трансформации
            }
            else
            {
                MessageBox.Show("Введите корректные значения для перемещения по X, Y и Z", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Rotate_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(TransformValueX.Text, out double angle) && angle >= 0 && angle <= 360)
            {
                // Параметры оси вращения
                Vector3D axis = new Vector3D(1, 0, 0); // По умолчанию вращение вокруг X
                if (double.TryParse(TransformValueY.Text, out double axisY) && axisY != 0)
                {
                    axis = new Vector3D(0, 1, 0); // Вращение вокруг Y
                }
                else if (double.TryParse(TransformValueZ.Text, out double axisZ) && axisZ != 0)
                {
                    axis = new Vector3D(0, 0, 1); // Вращение вокруг Z
                }

                var rotateTransform = new RotateTransform3D(new AxisAngleRotation3D(axis, angle));
                _transformGroup.Children.Add(rotateTransform); // Добавляем к группе трансформаций
                UpdateCurrentTransformationMatrix(rotateTransform.Value); // Обновляем матрицу
                ApplyTransform(); // Применяем все трансформации
            }
            else
            {
                MessageBox.Show("Введите угол вращения от 0 до 360", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ApplyTransform()
        {
            foreach (var visual in helixViewport.Children)
            {
                if (visual is ModelVisual3D modelVisual)
                {
                    modelVisual.Transform = _transformGroup; // Применяем группу трансформаций
                }
            }
            DisplayMatrix(_currentTransformationMatrix); // Отображаем текущую матрицу
        }

        private void UpdateCurrentTransformationMatrix(Matrix3D newTransform)
        {
            // Принцип можно поменять, если нужно иметь другую логику матрицы
            _currentTransformationMatrix = _currentTransformationMatrix * newTransform; // Обновляем текущую матрицу
        }

        private void DisplayMatrix(Matrix3D matrix)
        {
            MatrixOutput.Text = $"[{Environment.NewLine}" +
                                $" {matrix.M11:F2}, {matrix.M12:F2}, {matrix.M13:F2}, {matrix.OffsetX:F2}, {Environment.NewLine}" +
                                $" {matrix.M21:F2}, {matrix.M22:F2}, {matrix.M23:F2}, {matrix.OffsetY:F2}, {Environment.NewLine}" +
                                $" {matrix.M31:F2}, {matrix.M32:F2}, {matrix.M33:F2}, {matrix.OffsetZ:F2}, {Environment.NewLine}" +
                                $" 0, 0, 0, 1{Environment.NewLine}" +
                                $"]";
        }

        private void CreateProjectionsButton_Click(object sender, RoutedEventArgs e)
        {
            CreateProjection(xyPlot, "XY", p => new ScatterPoint(p.X, p.Y));
            CreateProjection(xzPlot, "XZ", p => new ScatterPoint(p.X, p.Z));
            CreateProjection(yzPlot, "YZ", p => new ScatterPoint(p.Y, p.Z));
        }

        private Point3DCollection ExtractPoints(Model3DGroup modelGroup)
        {
            var points = new Point3DCollection();
            foreach (var model in modelGroup.Children)
            {
                if (model is GeometryModel3D geometryModel)
                {
                    if (geometryModel.Geometry is MeshGeometry3D mesh)
                    {
                        foreach (var point in mesh.Positions)
                        {
                            points.Add(point);
                        }
                    }
                }
            }
            return points;
        }

        private void CreateProjection(PlotView plotView, string title, Func<Point3D, ScatterPoint> selector)
        {
            if (originalPoints == null) return; // Проверка, загружены ли оригинальные точки

            var plotModel = new PlotModel { Title = title };
            var scatterSeries = new ScatterSeries();

            // Применение трансформации к оригинальным точкам
            foreach (var point in originalPoints)
            {
                var transformedPoint = _transformGroup.Transform(point); // Применяем трансформацию
                scatterSeries.Points.Add(selector(transformedPoint)); // Добавляем преобразованную точку
            }
            plotModel.Series.Add(scatterSeries);
            plotView.Model = plotModel;
        }
    }
}