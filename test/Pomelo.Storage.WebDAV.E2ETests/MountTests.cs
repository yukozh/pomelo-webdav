namespace Pomelo.Storage.WebDAV.E2ETests
{
    public class MountTests : TestBase
    {
        [Fact]
        public void CreateFolderTest()
        {
            // Arrange
            var folderName = "test_create_folder";

            // Act
            Directory.CreateDirectory(Path.Combine(Drive, folderName));

            // Assert
            Assert.True(Directory.Exists(Path.Combine(Drive, folderName)));
            Assert.True(Directory.Exists(Path.Combine(StoragePath, folderName)));
        }

        [Fact]
        public void DeleteEmptyFolderTest()
        {
            // Arrange
            var folderName = "test_delete_empty_folder";
            Directory.CreateDirectory(Path.Combine(Drive, folderName));

            // Act
            Directory.Delete(Path.Combine(Drive, folderName));

            // Assert
            Assert.False(Directory.Exists(Path.Combine(Drive, folderName)));
            Assert.False(Directory.Exists(Path.Combine(StoragePath, folderName)));
        }

        [Fact]
        public void CreateFileTest()
        {
            // Arrange
            var fileName = "test_create_file.txt";

            // Act
            File.WriteAllText(Path.Combine(Drive, fileName), "Hello World!");

            // Assert
            Assert.True(File.Exists(Path.Combine(Drive, fileName)));
            Assert.True(File.Exists(Path.Combine(StoragePath, fileName)));
            Assert.Equal("Hello World!", File.ReadAllText(Path.Combine(StoragePath, fileName)));
        }

        [Fact]
        public void ReadFileTest()
        {
            // Arrange
            var fileName = "test_read_file.txt";
            File.WriteAllText(Path.Combine(StoragePath, fileName), "Hello World!");

            // Act
            var text = File.ReadAllText(Path.Combine(Drive, fileName));

            // Assert
            Assert.Equal("Hello World!", text);
        }

        [Fact]
        public void CopyFileTest()
        {
            // Arrange
            var srcFileName = "test_copy_file.txt";
            var destFileName = "test_copy_file2.txt";
            File.WriteAllText(Path.Combine(Drive, srcFileName), "Hello World!");

            // Act
            File.Copy(Path.Combine(Drive, srcFileName), Path.Combine(Drive, destFileName));

            // Assert
            Assert.Equal("Hello World!", File.ReadAllText(Path.Combine(Drive, destFileName)));
        }

        [Fact]
        public void CopyFileConflictTest_Throw()
        {
            // Arrange
            var srcFileName = "test_copy_file3.txt";
            var destFileName = "test_copy_file4.txt";
            File.WriteAllText(Path.Combine(Drive, srcFileName), "Hello World!");
            File.WriteAllText(Path.Combine(Drive, destFileName), "Original text.");

            // Act & Assert
            Assert.Throws<IOException>(() =>
            {
                File.Copy(Path.Combine(Drive, srcFileName), Path.Combine(Drive, destFileName), false);
            });
            Assert.Equal("Original text.", File.ReadAllText(Path.Combine(Drive, destFileName)));
        }

        [Fact]
        public void CopyFileConflictTest_Overwrite()
        {
            // Arrange
            var srcFileName = "test_copy_file5.txt";
            var destFileName = "test_copy_file6.txt";
            File.WriteAllText(Path.Combine(Drive, srcFileName), "Hello World!");
            File.WriteAllText(Path.Combine(Drive, destFileName), "Original text.");

            // Act
            File.Copy(Path.Combine(Drive, srcFileName), Path.Combine(Drive, destFileName), true);

            // Assert
            Assert.Equal("Hello World!", File.ReadAllText(Path.Combine(Drive, destFileName)));
        }

        [Fact]
        public void MoveFileTest()
        {
            // Arrange
            var srcFileName = "test_move_file.txt";
            var destFileName = "test_move_file2.txt";
            File.WriteAllText(Path.Combine(Drive, srcFileName), "Hello World!");

            // Act
            File.Move(Path.Combine(Drive, srcFileName), Path.Combine(Drive, destFileName));

            // Assert
            Assert.True(File.Exists(Path.Combine(Drive, destFileName)));
            Assert.False(File.Exists(Path.Combine(Drive, srcFileName)));
        }

        [Fact]
        public void MoveFileConflictTest_Throw()
        {
            // Arrange
            var srcFileName = "test_move_file.txt";
            var destFileName = "test_move_file2.txt";
            File.WriteAllText(Path.Combine(Drive, srcFileName), "Hello World!");
            File.WriteAllText(Path.Combine(Drive, destFileName), "Original text.");

            // Act & Assert
            Assert.Throws<IOException>(() =>
            {
                File.Move(Path.Combine(Drive, srcFileName), Path.Combine(Drive, destFileName), false);
            });
            Assert.Equal("Original text.", File.ReadAllText(Path.Combine(Drive, destFileName)));
        }

        [Fact]
        public void MoveFileConflictTest_Overwrite()
        {
            // Arrange
            var srcFileName = "test_move_file3.txt";
            var destFileName = "test_move_file4.txt";
            File.WriteAllText(Path.Combine(Drive, srcFileName), "Hello World!");
            File.WriteAllText(Path.Combine(Drive, destFileName), "Original text.");

            // Act
            File.Move(Path.Combine(Drive, srcFileName), Path.Combine(Drive, destFileName), true);

            // Assert
            Assert.Equal("Hello World!", File.ReadAllText(Path.Combine(Drive, destFileName)));
        }

        [Fact]
        public void MoveFolderTest()
        {
            // Arrange
            var srcDirectoryName = "dir1";
            var destDirectoryName = "dir2";
            Directory.CreateDirectory(Path.Combine(Drive, srcDirectoryName));
            File.WriteAllText(Path.Combine(Drive, srcDirectoryName, "test.txt"), "Hello World!");

            // Act
            Directory.Move(Path.Combine(Drive, srcDirectoryName), Path.Combine(Drive, destDirectoryName));

            // Assert
            Assert.False(Directory.Exists(Path.Combine(Drive, srcDirectoryName)));
            Assert.True(Directory.Exists(Path.Combine(Drive, destDirectoryName)));
            Assert.True(File.Exists(Path.Combine(Drive, destDirectoryName, "test.txt")));
            Assert.Equal("Hello World!", File.ReadAllText(Path.Combine(Drive, destDirectoryName, "test.txt")));
        }

        [Fact]
        public void MoveFolderConflictTest_Throw()
        {
            // Arrange
            var srcDirectoryName = "dir3";
            var destDirectoryName = "dir4";
            Directory.CreateDirectory(Path.Combine(Drive, srcDirectoryName));
            Directory.CreateDirectory(Path.Combine(Drive, destDirectoryName));
            File.WriteAllText(Path.Combine(Drive, srcDirectoryName, "test.txt"), "Hello World!");

            // Act & Assert
            Assert.Throws<IOException>(() =>
            {
                Directory.Move(Path.Combine(Drive, srcDirectoryName), Path.Combine(Drive, destDirectoryName));
            });
        }

        [Fact]
        public void InvalidCharactorTest()
        {
            // Act & Assert
            Assert.Throws<IOException>(() => 
            {
                Directory.CreateDirectory(Path.Combine(Drive, "?"));
            });
            Assert.Throws<IOException>(() =>
            {
                Directory.CreateDirectory(Path.Combine(Drive, ">"));
            });
            Assert.Throws<IOException>(() =>
            {
                Directory.CreateDirectory(Path.Combine(Drive, "<"));
            });
            Assert.Throws<IOException>(() =>
            {
                Directory.CreateDirectory(Path.Combine(Drive, "|"));
            });
            Assert.Throws<IOException>(() =>
            {
                File.WriteAllText(Path.Combine(Drive, "?"), "");
            });
            Assert.Throws<IOException>(() =>
            {
                File.WriteAllText(Path.Combine(Drive, ">"), "");
            });
            Assert.Throws<IOException>(() =>
            {
                File.WriteAllText(Path.Combine(Drive, "<"), "");
            });
            Assert.Throws<IOException>(() =>
            {
                File.WriteAllText(Path.Combine(Drive, "|"), "");
            });
        }

        [Fact]
        public void ChineseCharactorTests()
        {
            // Arrange
            var srcDirectoryName = "中文路径1";
            var destDirectoryName = "中文路径2";
            Directory.CreateDirectory(Path.Combine(Drive, srcDirectoryName));
            File.WriteAllText(Path.Combine(Drive, srcDirectoryName, "测试.txt"), "你好，世界！");

            // Act
            Directory.Move(Path.Combine(Drive, srcDirectoryName), Path.Combine(Drive, destDirectoryName));

            // Assert
            Assert.False(Directory.Exists(Path.Combine(Drive, srcDirectoryName)));
            Assert.True(Directory.Exists(Path.Combine(Drive, destDirectoryName)));
            Assert.True(File.Exists(Path.Combine(Drive, destDirectoryName, "测试.txt")));
            Assert.Equal("你好，世界！", File.ReadAllText(Path.Combine(Drive, destDirectoryName, "测试.txt")));
            Assert.Throws<IOException>(() =>
            {
                Directory.Delete(Path.Combine(Drive, destDirectoryName), false);
            });

            // Act
            Directory.Delete(Path.Combine(Drive, destDirectoryName), true);

            // Assert
            Assert.False(Directory.Exists(Path.Combine(Drive, destDirectoryName)));
        }
    }
}