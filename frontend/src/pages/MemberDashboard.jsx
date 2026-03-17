import { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { 
  Sparkles, 
  CheckCircle2, 
  Clock, 
  LogOut,
  Layout,
  Star,
  Zap,
  CheckCircle,
  Calendar
} from 'lucide-react';
import { taskService, authService } from '../services/api';
import { formatDate, cn } from '../lib/utils';

const MemberDashboard = ({ user, onLogout }) => {
  const [tasks, setTasks] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchTasks();
  }, []);

  const fetchTasks = async () => {
    try {
      const res = await taskService.getMyTasks();
      setTasks(res.data);
    } catch (err) {
      console.error("Failed to fetch tasks", err);
    } finally {
      setLoading(false);
    }
  };

  const handleToggleComplete = async (taskId) => {
    try {
      await taskService.completeTask(taskId);
      setTasks(tasks.map(t => 
        t.id === taskId ? { ...t, status: t.status === 'Completed' ? 'InProgress' : 'Completed' } : t
      ));
    } catch (err) {
      console.error("Failed to update task", err);
    }
  };

  const completedCount = tasks.filter(t => t.status === 'Completed').length;
  const pendingCount = tasks.length - completedCount;

  return (
    <div className="min-h-screen bg-transparent text-text flex flex-col relative">
      {/* Top Nav */}
      <motion.header 
        initial={{ y: -50, opacity: 0 }}
        animate={{ y: 0, opacity: 1 }}
        className="bg-white/60 backdrop-blur-xl border-b border-primary/10 px-8 py-4 flex items-center justify-between sticky top-0 z-30 shadow-sm"
      >
        <div className="flex items-center gap-3">
          <div className="relative w-10 h-10 flex items-center justify-center group">
            <div className="absolute inset-0 bg-primary rounded-xl blur-md opacity-30 group-hover:opacity-60 transition-opacity"></div>
            <div className="relative w-8 h-8 bg-gradient-to-br from-primary to-secondary rounded-lg flex items-center justify-center shadow-lg shadow-primary/20">
              <Sparkles className="text-white" size={16} />
            </div>
          </div>
          <span className="text-xl font-heading font-extrabold tracking-tight bg-clip-text text-transparent bg-gradient-to-r from-primary to-secondary">Manager AI</span>
        </div>
        
        <div className="flex items-center gap-6">
          <div className="flex items-center gap-3">
             <div className="text-right hidden sm:block">
                <p className="text-sm font-bold text-text truncate">{user.name}</p>
                <p className="text-[10px] font-bold text-primary uppercase tracking-wider">{user.role}</p>
             </div>
             <div className="w-10 h-10 rounded-full bg-gradient-to-br from-primary to-secondary flex items-center justify-center text-white font-extrabold shadow-sm">
                {user.name.charAt(0)}
             </div>
          </div>
          <button 
            onClick={onLogout}
            className="p-2 hover:bg-white text-text-muted hover:text-red-500 rounded-xl transition-all"
            title="Sign Out"
          >
            <LogOut size={20} />
          </button>
        </div>
      </motion.header>

      <main className="max-w-5xl mx-auto w-full p-8 md:p-12 relative z-10">
        <motion.div 
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          className="mb-12"
        >
          <h1 className="text-4xl font-heading font-extrabold mb-2 text-text">My Tasks</h1>
          <p className="text-text-muted font-medium flex items-center gap-2">
            You have <span className="text-primary font-bold bg-primary/10 px-2 py-0.5 rounded-md">{pendingCount} pending</span> tasks assigned by AI agents.
          </p>
        </motion.div>

        {/* Mini Stats */}
        <motion.div 
          initial={{ opacity: 0, scale: 0.95 }}
          animate={{ opacity: 1, scale: 1 }}
          transition={{ delay: 0.2 }}
          className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-12"
        >
          <div className="glass-card p-6 border-l-4 border-l-primary flex items-center gap-4 hover:-translate-y-1 transition-transform">
             <div className="w-12 h-12 rounded-2xl bg-primary/10 border border-primary/20 flex items-center justify-center text-primary shadow-sm">
                <Layout size={24} />
             </div>
             <div>
                <p className="text-xs font-bold text-text-muted uppercase tracking-widest">Total</p>
                <p className="text-2xl font-bold font-heading">{tasks.length}</p>
             </div>
          </div>
          <div className="glass-card p-6 border-l-4 border-l-orange-500 flex items-center gap-4 hover:-translate-y-1 transition-transform">
             <div className="w-12 h-12 rounded-2xl bg-orange-100 border border-orange-200 flex items-center justify-center text-orange-500 shadow-sm">
                <Clock size={24} />
             </div>
             <div>
                <p className="text-xs font-bold text-text-muted uppercase tracking-widest">Pending</p>
                <p className="text-2xl font-bold font-heading">{pendingCount}</p>
             </div>
          </div>
          <div className="glass-card p-6 border-l-4 border-l-green-500 flex items-center gap-4 hover:-translate-y-1 transition-transform">
             <div className="w-12 h-12 rounded-2xl bg-green-100 border border-green-200 flex items-center justify-center text-green-500 shadow-sm">
                <CheckCircle size={24} />
             </div>
             <div>
                <p className="text-xs font-bold text-text-muted uppercase tracking-widest">Done</p>
                <p className="text-2xl font-bold font-heading">{completedCount}</p>
             </div>
          </div>
        </motion.div>

        {/* Task List */}
        <div className="space-y-4">
          {loading ? (
             <div className="p-20 flex flex-col items-center gap-4 text-text-muted">
                <Clock className="animate-spin text-primary" size={40} />
                <p className="font-bold animate-pulse">Syncing with agents...</p>
             </div>
          ) : tasks.length === 0 ? (
            <motion.div 
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              className="p-20 text-center glass-card border-dashed border-primary/20 bg-white/50"
            >
              <Zap className="mx-auto text-primary mb-4 animate-bounce drop-shadow-[0_4px_10px_rgba(217,70,239,0.3)]" size={48} />
              <h3 className="text-xl font-bold mb-2 text-text">Workspace Empty</h3>
              <p className="text-text-muted">Nothing assigned to you currently. Take some rest!</p>
            </motion.div>
          ) : tasks.map((task, idx) => (
             <motion.div
               key={task.id}
               initial={{ opacity: 0, x: -20 }}
               animate={{ opacity: 1, x: 0 }}
               transition={{ delay: 0.3 + idx * 0.05 }}
               whileHover={{ x: 5, scale: 1.01 }}
               className={cn(
                 "glass-card p-6 flex items-center gap-6 border-l-4 transition-all hover:shadow-[0_8px_30px_rgba(217,70,239,0.15)]",
                 task.status === 'Completed' ? "border-l-green-500 opacity-60 bg-white/40" : "border-l-primary bg-white/80"
               )}
             >
                <button 
                  onClick={() => handleToggleComplete(task.id)}
                  className={cn(
                    "w-8 h-8 rounded-full border-2 flex items-center justify-center transition-all shadow-inner",
                    task.status === 'Completed' ? "bg-green-500 border-green-500 text-white shadow-[0_0_10px_rgba(34,197,94,0.3)]" : "bg-white border-primary/20 text-transparent hover:border-primary hover:bg-primary/5 hover:shadow-[0_0_10px_rgba(217,70,239,0.2)]"
                  )}
                >
                  <CheckCircle size={18} fill="currentColor" />
                </button>
                
                <div className="flex-grow">
                  <h3 className={cn(
                    "text-lg font-bold transition-all line-clamp-1",
                    task.status === 'Completed' ? "line-through text-text-muted" : "text-text"
                  )}>
                    {task.title || task.name}
                  </h3>
                  <div className="flex items-center gap-4 mt-1 text-sm font-medium text-text-muted">
                    <span className="flex items-center gap-1"><Star size={14} className="text-orange-400 drop-shadow-sm" /> {task.category || 'Core'}</span>
                    <span className="flex items-center gap-1"><Calendar size={14} className="text-primary" /> Assigned {formatDate(task.assignedAt)}</span>
                  </div>
                </div>

                <div className={cn(
                   "px-4 py-1.5 rounded-full text-xs font-bold uppercase tracking-widest border backdrop-blur-md",
                   task.status === 'Completed' ? "bg-green-100 text-green-600 border-green-200" : "bg-primary/10 text-primary border-primary/20 shadow-sm"
                )}>
                   {task.status}
                </div>
             </motion.div>
          ))}
        </div>
      </main>
    </div>
  );
};

export default MemberDashboard;
