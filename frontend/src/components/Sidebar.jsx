import { useNavigate, useLocation, Link } from 'react-router-dom';
import { motion } from 'framer-motion';
import { 
  Sparkles, 
  BarChart3, 
  CheckCircle2, 
  Users, 
  Activity, 
  LogOut,
  LayoutDashboard,
  Kanban,
  Briefcase
} from 'lucide-react';
import { cn } from '../lib/utils';

const Sidebar = ({ user, onLogout }) => {
  const location = useLocation();
  const navigate = useNavigate();

  const menuItems = [
    { icon: <LayoutDashboard size={20} />, label: "Overview", path: "/dashboard" },
    { icon: <Briefcase size={20} />, label: "Projects", path: "/projects" },
    { icon: <Kanban size={20} />, label: "Kanban", path: "/kanban" },
    { icon: <Users size={20} />, label: "Team", path: "/team" },
    { icon: <Activity size={20} />, label: "Reports", path: "/reports" },
  ];

  return (
    <motion.aside 
      initial={{ x: -300 }}
      animate={{ x: 0 }}
      transition={{ duration: 0.5, ease: "easeOut" }}
      className="w-72 bg-white/60 backdrop-blur-xl border-r border-primary/10 p-8 flex flex-col fixed h-full z-20 shadow-[4px_0_24px_rgba(217,70,239,0.05)]"
    >
      <motion.div 
        whileHover={{ scale: 1.05 }}
        whileTap={{ scale: 0.95 }}
        className="flex items-center gap-3 mb-12 cursor-pointer group" 
        onClick={() => navigate('/')}
      >
        <div className="relative w-10 h-10 flex items-center justify-center">
           <div className="absolute inset-0 bg-primary rounded-xl blur-md opacity-30 group-hover:opacity-60 transition-opacity"></div>
           <div className="relative w-10 h-10 bg-gradient-to-br from-primary to-secondary rounded-xl flex items-center justify-center shadow-lg shadow-primary/20">
             <Sparkles className="text-white" size={20} />
           </div>
        </div>
        <span className="text-2xl font-heading font-extrabold tracking-tight bg-clip-text text-transparent bg-gradient-to-r from-primary to-secondary">Manager AI</span>
      </motion.div>

      <nav className="space-y-2 flex-grow">
        {menuItems.map((item) => {
          const isActive = location.pathname === item.path;
          return (
            <Link
              key={item.path}
              to={item.path}
              className={cn(
                "flex items-center gap-3 px-4 py-3.5 rounded-2xl text-sm font-bold transition-all relative overflow-hidden group",
                isActive 
                  ? "text-primary shadow-sm" 
                  : "text-text-muted hover:text-primary"
              )}
            >
              {isActive && (
                <motion.div
                  layoutId="activeTab"
                  className="absolute inset-0 bg-primary/10 border border-primary/20 rounded-2xl"
                  initial={false}
                  transition={{ type: "spring", stiffness: 300, damping: 30 }}
                />
              )}
              <span className="relative z-10 flex items-center gap-3">
                {item.icon}
                {item.label}
              </span>
            </Link>
          );
        })}
      </nav>

      <div className="pt-8 border-t border-primary/10 mt-auto">
        <div className="flex items-center gap-3 mb-6 p-3 rounded-2xl bg-white/60 border border-primary/10 backdrop-blur-sm hover:bg-white transition-colors">
          <div className="w-10 h-10 rounded-full bg-gradient-to-br from-primary to-secondary flex items-center justify-center text-white font-bold shadow-sm">
            {user?.name?.charAt(0) || 'U'}
          </div>
          <div className="flex-grow overflow-hidden text-left">
            <p className="text-sm font-bold text-text truncate">{user?.name || 'User'}</p>
            <p className="text-[10px] font-bold text-primary uppercase tracking-wider">{user?.role || 'Role'}</p>
          </div>
        </div>
        <button 
          onClick={onLogout}
          className="flex items-center gap-2 text-text-muted hover:text-red-500 transition-colors font-semibold text-sm w-full px-2"
        >
          <LogOut size={18} /> Sign Out
        </button>
      </div>
    </motion.aside>
  );
};

export default Sidebar;
