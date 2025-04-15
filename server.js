const express = require('express');
const bodyParser = require('body-parser');
const cors = require('cors');
const mongoose = require('mongoose');  // 引入 mongoose

const app = express();
const PORT = process.env.PORT || 3000;

// 使用中间件
app.use(bodyParser.urlencoded({ extended: false }));
app.use(bodyParser.json());
app.use(cors());

// 连接到本地 MongoDB 实例（数据库名为 user_database）
// 确保你的 MongoDB 已经启动，并监听在默认端口 27017
mongoose.connect("mongodb://localhost:27017/user_database");

const db = mongoose.connection;
db.on("error", console.error.bind(console, "MongoDB connection error:"));
db.once("open", () => {
  console.log("Connected to MongoDB");
});

// 定义用户的 Schema 和 Model
const userSchema = new mongoose.Schema({
  username: { type: String, required: true, unique: true },
  password: { type: String, required: true }
});
const User = mongoose.model("User", userSchema);

// 示例登录接口（已有示例，此处修改为使用数据库验证）
app.post('/api/login', async (req, res) => {
  const { username, password } = req.body;
  try {
    const user = await User.findOne({ username });
    if (!user) {
      return res.status(401).json({ success: false, message: "用户不存在" });
    }
    // 注意：实际项目中应使用密码加密及比较，这里仅作明文比较示例
    if (user.password === password) {
      res.status(200).json({ success: true, message: "登录成功" });
    } else {
      res.status(401).json({ success: false, message: "用户名或密码错误" });
    }
  } catch (error) {
    res.status(500).json({ success: false, message: error.message });
  }
});

// 新增注册接口
app.post('/api/signup', async (req, res) => {
  const { username, password } = req.body;
  
  // 基本字段验证
  if (!username || !password) {
    return res.status(400).json({ success: false, message: "用户名和密码不能为空" });
  }
  
  try {
    // 查询数据库是否已存在同名用户
    const existingUser = await User.findOne({ username });
    if (existingUser) {
      return res.status(400).json({ success: false, message: "用户名已存在" });
    }
    
    // 创建新用户（实际项目中应对密码进行加密处理）
    const newUser = new User({ username, password });
    await newUser.save();
    console.log(`注册账号：${username}`);
    res.status(200).json({ success: true, message: "注册成功" });
  } catch (error) {
    res.status(500).json({ success: false, message: error.message });
  }
});

app.listen(PORT, () => {
  console.log(`Server is running on port ${PORT}`);
});
