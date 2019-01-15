import React, { Component } from 'react';
import {withRouter} from 'react-router';
import './Blogs.css';

class Blogs extends Component {
  displayName = Blogs.name

  constructor(props) {
    super(props);
    this.state = { blogs: [], loading: true };

    this.handleClick = this.handleClick.bind(this);

    fetch(process.env.REACT_APP_API_ROOT + '/api/blogs')
    .then(response => response.json())
    .then(data => {
        this.setState({ blogs: data, loading: false });
    });
  }

  handleClick(e) {
      var blogname = e.target.parentElement.getAttribute('data-blog');
      console.log(blogname);
      this.props.history.push('/fetchdata/' + blogname);
  }

  renderBlogsTable(blogs) {
    //var self = this;
    var rows = blogs.map(function(blog) {
        return (
          <tr key={blog.Name} data-blog={blog.Name} onClick={this.handleClick}>
          <td>{blog.Name}</td>
          <td>{blog.Title}</td>
          <td>{blog.Photos}</td>
          <td>{blog.Videos}</td>
          <td>{blog.TotalPosts}</td>
          </tr>
        );}.bind(this));

    return (
      <table className='table'>
        <thead>
          <tr>
            <th>Name</th>
            <th>Title</th>
            <th>Photos</th>
            <th>Videos</th>
            <th>Total</th>
          </tr>
        </thead>
        <tbody>
          {rows}
        </tbody>
      </table>
    );
  }

  render() {
    let contents = this.state.loading
      ? <p><em>Loading...</em></p>
        : this.renderBlogsTable(this.state.blogs);

    return (
      <div>
        <h1>Blogs available</h1>
        {contents}
      </div>
    );
  }
}

export default withRouter(Blogs);
