import { useState, useEffect } from 'react';
import Sidebar from '../components/Sidebar';
import { Kanban as KanbanIcon, Plus, X, Search, MoreHorizontal, User, Clock, Filter, CheckCircle2, AlertCircle } from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';
import { cn, formatDate } from '../lib/utils';
import { taskService, authService } from '../services/api';

const KanbanPage = ({ user, onLogout }) => {
  const [tasks, setTasks] = useState([]);
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [newTask, setNewTask] = useState({
    title: '',
    description: '',
    priority: 'Medium',
    assignedUser: ''
  });

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      const [tasksRes, usersRes] = await Promise.all([
        taskService.getTasks(),
        authService.getUsers()
      ]);
      setTasks(tasksRes.data);
      setUsers(usersRes.data);
    } catch (err) {
      console.error("Failed to fetch Kanban data", err);
    } finally {
      setLoading(false);
    }
  };

  const handleAddTask = async (e) => {
    e.preventDefault();
    try {
      const res = await taskService.createTask(newTask);
      setTasks([...tasks, res.data]);
      setIsModalOpen(false);
      setNewTask({ title: '', description: '', priority: 'Medium', assignedUser: '' });
    } catch (err) {
      console.error("Failed to create task", err);
    }
  };

  const getTasksByStatus = (status) => {
    return tasks.filter(t => {
      const s = t.status?.toString();
      if (status === 'To Do') return s === 'NotStarted' || s === '0';
      if (status === 'In Progress') return s === 'InProgress' || s === '1';
      if (status === 'Completed') return s === 'Completed' || s === '2';
      return false;
    });
  };

  return (
    <div className="min-h-screen bg-transparent flex relative text-text">
      <Sidebar user={user} onLogout={onLogout} />
      
      <main className="flex-grow ml-72 p-12 relative z-10 overflow-y-auto h-screen">
        <motion.header 
          initial={{ opacity: 0, y: -20 }}
          animate={{ opacity: 1, y: 0 }}
          className="mb-10 flex items-end justify-between"
        >
          <div>
            <div className="flex items-center gap-4 mb-2">
              <div className="w-12 h-12 rounded-2xl bg-primary/10 flex items-center justify-center text-primary shadow-sm border border-primary/20">
                 <KanbanIcon size={24} />
              </div>
              <h1 className="text-4xl font-heading font-extrabold bg-clip-text text-transparent bg-gradient-to-r from-primary to-secondary">Workflow Board</h1>
            </div>
            <p className="text-text-muted font-medium ml-1">Coordinate and track tasks across all active projects.</p>
          </div>

          <div className="flex items-center gap-4">
             <div className="relative group hidden md:block">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-text-muted transition-colors group-focus-within:text-primary" size={18} />
                <input 
                  type="text" 
                  placeholder="Search tasks..." 
                  className="bg-white/50 backdrop-blur-md border border-primary/10 rounded-xl py-2.5 pl-10 pr-4 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 w-64 transition-all"
                />
             </div>
             <motion.button 
               whileHover={{ scale: 1.02 }}
               whileTap={{ scale: 0.98 }}
               onClick={() => setIsModalOpen(true)}
               className="btn-primary flex items-center gap-2"
             >
               <Plus size={18} /> New Task
             </motion.button>
          </div>
        </motion.header>

        {loading ? (
          <div className="h-[60vh] flex flex-col items-center justify-center gap-4 text-text-muted">
             <Clock className="animate-spin text-primary" size={40} />
             <p className="font-bold animate-pulse">Organizing Workspace...</p>
          </div>
        ) : (
          <motion.div 
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ delay: 0.2 }}
            className="grid grid-cols-1 md:grid-cols-3 gap-8 items-start"
          >
              <KanbanColumn 
                title="To Do" 
                tasks={getTasksByStatus('To Do')} 
                delay={0.3} 
                color="primary" 
                icon={<Filter size={16} />}
              />
              <KanbanColumn 
                title="In Progress" 
                tasks={getTasksByStatus('In Progress')} 
                delay={0.4} 
                color="orange" 
                icon={<Clock size={16} />}
              />
              <KanbanColumn 
                title="Completed" 
                tasks={getTasksByStatus('Completed')} 
                delay={0.5} 
                color="green" 
                icon={<CheckCircle2 size={16} />}
              />
          </motion.div>
        )}
      </main>

      {/* Add Task Modal */}
      <AnimatePresence>
        {isModalOpen && (
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <motion.div 
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              onClick={() => setIsModalOpen(false)}
              className="absolute inset-0 bg-black/40 backdrop-blur-sm"
            />
            <motion.div 
              initial={{ opacity: 0, scale: 0.9, y: 20 }}
              animate={{ opacity: 1, scale: 1, y: 0 }}
              exit={{ opacity: 0, scale: 0.9, y: 20 }}
              className="glass-card w-full max-w-lg relative z-10 border-primary/20 shadow-2xl overflow-hidden"
            >
              <div className="p-8 border-b border-primary/10 flex items-center justify-between bg-gradient-to-r from-primary/5 to-transparent">
                <h3 className="text-2xl font-heading font-extrabold text-text flex items-center gap-3">
                  <div className="w-10 h-10 rounded-xl bg-primary/10 flex items-center justify-center text-primary shadow-sm">
                    <Plus size={20} />
                  </div>
                  New Workspace Task
                </h3>
                <button 
                  onClick={() => setIsModalOpen(false)}
                  className="p-2 hover:bg-primary/10 rounded-xl transition-colors text-text-muted hover:text-primary"
                >
                  <X size={20} />
                </button>
              </div>
              
              <form onSubmit={handleAddTask} className="p-8 space-y-6">
                <div className="space-y-2">
                  <label className="text-xs font-bold text-text-muted uppercase tracking-widest ml-1">Task Title</label>
                  <input 
                    required
                    value={newTask.title}
                    onChange={(e) => setNewTask({...newTask, title: e.target.value})}
                    placeholder="E.g. Design System Implementation"
                    className="w-full bg-white/50 border border-primary/10 rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 transition-all font-medium"
                  />
                </div>
                
                <div className="space-y-2">
                  <label className="text-xs font-bold text-text-muted uppercase tracking-widest ml-1">Description</label>
                  <textarea 
                    value={newTask.description}
                    onChange={(e) => setNewTask({...newTask, description: e.target.value})}
                    placeholder="Describe what needs to be done..."
                    className="w-full bg-white/50 border border-primary/10 rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 transition-all min-h-[100px] resize-none font-medium"
                  />
                </div>

                <div className="grid grid-cols-2 gap-6">
                  <div className="space-y-2">
                    <label className="text-xs font-bold text-text-muted uppercase tracking-widest ml-1">Priority</label>
                    <select 
                      value={newTask.priority}
                      onChange={(e) => setNewTask({...newTask, priority: e.target.value})}
                      className="w-full bg-white/50 border border-primary/10 rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 appearance-none font-bold text-primary"
                    >
                      <option>Low</option>
                      <option>Medium</option>
                      <option>High</option>
                      <option>Critical</option>
                    </select>
                  </div>
                  <div className="space-y-2">
                    <label className="text-xs font-bold text-text-muted uppercase tracking-widest ml-1">Assignee</label>
                    <select 
                      value={newTask.assignedUser}
                      onChange={(e) => setNewTask({...newTask, assignedUser: e.target.value})}
                      className="w-full bg-white/50 border border-primary/10 rounded-xl px-4 py-3 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 appearance-none font-bold text-primary"
                    >
                      <option value="">Unassigned</option>
                      {users.map(u => (
                        <option key={u.id} value={u.name}>{u.name}</option>
                      ))}
                    </select>
                  </div>
                </div>

                <div className="pt-4 flex gap-4">
                  <button 
                    type="button"
                    onClick={() => setIsModalOpen(false)}
                    className="flex-1 btn-secondary"
                  >
                    Cancel
                  </button>
                  <button 
                    type="submit"
                    className="flex-1 btn-primary"
                  >
                    Create Task
                  </button>
                </div>
              </form>
            </motion.div>
          </div>
        )}
      </AnimatePresence>
    </div>
  );
};

const KanbanColumn = ({ title, tasks, delay, color, icon }) => {
  const getBorderColor = () => {
    if (color === 'orange') return 'border-l-orange-500';
    if (color === 'green') return 'border-l-green-500';
    return 'border-l-primary';
  };

  const getTagStyle = () => {
    if (color === 'orange') return 'bg-orange-100 text-orange-600 border-orange-200';
    if (color === 'green') return 'bg-green-100 text-green-600 border-green-200';
    return 'bg-primary/10 text-primary border-primary/20';
  };

  return (
    <motion.div 
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ delay }}
      className="flex flex-col h-full min-h-[700px] glass-card bg-white/30 p-4 border border-primary/5 rounded-[2rem] shadow-sm overflow-hidden"
    >
        <div className="flex items-center justify-between mb-6 px-4 py-2">
            <div className="flex items-center gap-2">
              <span className={cn("p-1.5 rounded-lg border", getTagStyle())}>
                {icon}
              </span>
              <h3 className="font-bold text-text uppercase tracking-widest text-xs">{title}</h3>
            </div>
            <span className={cn("px-3 py-1 rounded-full border text-[10px] font-extrabold tracking-tighter", getTagStyle())}>
              {tasks.length}
            </span>
        </div>
        
        <div className="space-y-4 overflow-y-auto px-2 pb-4 h-full scrollbar-none">
            {tasks.length === 0 ? (
              <div className="p-8 bg-white/40 rounded-3xl border border-primary/5 border-dashed text-center flex flex-col items-center gap-3">
                  <div className="w-10 h-10 rounded-full bg-white/50 flex items-center justify-center text-text-muted">
                    {icon}
                  </div>
                  <p className="text-xs font-bold text-text-muted italic">Clear as day</p>
              </div>
            ) : tasks.map((task, idx) => (
              <motion.div
                key={task.id}
                initial={{ opacity: 0, scale: 0.95 }}
                animate={{ opacity: 1, scale: 1 }}
                transition={{ delay: delay + (idx * 0.1) }}
                whileHover={{ y: -4, scale: 1.02 }}
                className={cn(
                  "bg-white/80 backdrop-blur-sm p-5 rounded-2xl border border-primary/10 shadow-sm transition-all hover:shadow-xl hover:shadow-primary/5 select-none",
                  getBorderColor(),
                  "border-l-4"
                )}
              >
                 <div className="flex items-start justify-between mb-2">
                    <h4 className="font-bold text-sm text-text line-clamp-2 leading-snug">{task.name || task.title}</h4>
                    <button className="text-text-muted hover:text-primary p-1">
                      <MoreHorizontal size={14} />
                    </button>
                 </div>
                 
                 <p className="text-xs text-text-muted line-clamp-2 mb-4 font-medium leading-relaxed">
                   {task.description || "No description provided."}
                 </p>

                 <div className="flex items-center justify-between pt-4 border-t border-primary/5">
                    <div className="flex items-center gap-1.5">
                       <div className="w-6 h-6 rounded-full bg-gradient-to-br from-primary to-secondary flex items-center justify-center text-[10px] text-white font-black shadow-sm">
                          {task.assignedTo ? task.assignedTo.charAt(0) : '?'}
                       </div>
                       <span className="text-[10px] font-bold text-text-muted">
                        {task.assignedTo || "Unassigned"}
                       </span>
                    </div>

                    <div className={cn(
                      "px-2 py-0.5 rounded-lg text-[10px] font-black uppercase tracking-tighter border shadow-sm",
                      task.priority === 'Critical' ? "bg-red-50 text-red-500 border-red-100" :
                      task.priority === 'High' ? "bg-orange-50 text-orange-500 border-orange-100" :
                      "bg-blue-50 text-blue-500 border-blue-100"
                    )}>
                      {task.priority || 'Medium'}
                    </div>
                 </div>
              </motion.div>
            ))}
        </div>
    </motion.div>
  );
};

export default KanbanPage;
